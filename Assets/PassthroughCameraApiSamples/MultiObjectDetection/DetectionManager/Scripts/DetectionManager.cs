// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class DetectionManager : MonoBehaviour
    {
        [SerializeField] private WebCamTextureManager m_webCamTextureManager;

        [Header("Controls configuration")]
        [SerializeField] private OVRInput.RawButton m_actionButton = OVRInput.RawButton.A;

        [Header("Ui references")]
        [SerializeField] private DetectionUiMenuManager m_uiMenuManager;

        [Header("Placement configureation")]
        [SerializeField] private GameObject m_spwanMarker;
        [SerializeField] private EnvironmentRayCastSampleManager m_environmentRaycast;
        [SerializeField] private float m_spawnDistance = 0.25f;
        [SerializeField] private AudioSource m_placeSound;

        [Header("Sentis inference ref")]
        [SerializeField] private SentisInferenceRunManager m_runInference;
        [SerializeField] private SentisInferenceUiManager m_uiInference;
        [Space(10)]
        public UnityEvent<int> OnObjectsIdentified;

        private bool m_isPaused = true;
        private List<GameObject> m_spwanedEntities = new();
        private bool m_isStarted = false;
        private bool m_isSentisReady = false;
        private float m_delayPauseBackTime = 0;

        Camera m_mainCamera;

        // Konumland�rma i�in e�ik de�erleri
        private const float HorizontalThreshold = 0.15f; // %15 merkezden uzakla�ma
        private const float VerticalThreshold = 0.15f;   // %15 merkezden uzakla�ma
        private const float NearDistanceThreshold = 1.0f; // 1 metre yak�n�
        private const float FarDistanceThreshold = 3.0f;  // 3 metreden uza��

        #region Unity Functions
        private void Awake()
        {
            OVRManager.display.RecenteredPose += CleanMarkersCallBack;
            // Kamera referans�n� al�n
            // VR/AR uygulamalar�nda bu genellikle OVRCameraRig'deki merkez kamerad�r. 
            // `Camera.main` yerine en uygun kameray� bulman�z gerekebilir, ancak genel bir ba�lang�� i�in `Camera.main` kullan�l�r.
            m_mainCamera = Camera.main;
            if (m_mainCamera == null)
            {
                Debug.LogError("Ana Kamera (Camera.main) bulunamad�! Konumland�rma do�ru �al��mayabilir.");
            }
        }
        private IEnumerator Start()
        {
            // Wait until Sentis model is loaded
            var sentisInference = FindAnyObjectByType<SentisInferenceRunManager>();
            while (!sentisInference.IsModelLoaded)
            {
                yield return null;
            }
            m_isSentisReady = true;
        }

        private void Update()
        {
            // Get the WebCamTexture CPU image
            var hasWebCamTextureData = m_webCamTextureManager.WebCamTexture != null;

            if (!m_isStarted)
            {
                // Manage the Initial Ui Menu
                if (hasWebCamTextureData && m_isSentisReady)
                {
                    m_uiMenuManager.OnInitialMenu(m_environmentRaycast.HasScenePermission());
                    m_isStarted = true;
                }
            }
            else
            {
                // Press A button to spawn 3d markers
                if (OVRInput.GetUp(m_actionButton) && m_delayPauseBackTime <= 0)
                {
                    SpwanCurrentDetectedObjects();
                    Talk();
                }
                // Cooldown for the A button after return from the pause menu
                m_delayPauseBackTime -= Time.deltaTime;
                if (m_delayPauseBackTime <= 0)
                {
                    m_delayPauseBackTime = 0;
                }
            }

            // Not start a sentis inference if the app is paused or we don't have a valid WebCamTexture
            if (m_isPaused || !hasWebCamTextureData)
            {
                if (m_isPaused)
                {
                    // Set the delay time for the A button to return from the pause menu
                    m_delayPauseBackTime = 0.1f;
                }
                return;
            }

            // Run a new inference when the current inference finishes
            if (!m_runInference.IsRunning())
            {
                m_runInference.RunInference(m_webCamTextureManager.WebCamTexture);
            }
        }

        private void Talk()
        {
            string message = "Alg�lanan nesneler: ";

            // Art�k box.Center property'sini kullanabiliriz.
            foreach (var box in m_uiInference.BoxDrawn)
            {
                // box.Center property'si art�k mevcut.
                string position = GetObjectRelativePosition(box.Center, box.WorldPos);
                message += box.ClassName + " (" + position + "), ";
            }

            if (message.EndsWith(", "))
            {
                message = message.Substring(0, message.Length - 2);
                message += "."; // Sonuna nokta ekleyelim
            }

            print("TTS Mesaj�: " + message);

            AiOrchestrator.Instance.OnPlayerHitButton(message);
        }

        // --- Konum Hesaplama Fonksiyonu ---

        /// <summary>
        /// Nesnenin ekrandaki 2D ve 3D d�nya konumuna g�re g�receli konumunu d�nd�r�r.
        /// </summary>
        /// <param name="screenCenter">Nesnenin piksel cinsinden merkez ekran koordinat�.</param>
        /// <param name="worldPosition">Nesnenin 3D d�nya konumu (WorldPos).</param>
        /// <returns>�rn: "solda ve yak�nda"</returns>
        private string GetObjectRelativePosition(Vector2 centerOffsetPx, Vector3? worldPosition)
        {
            if (m_mainCamera == null || m_uiInference.DisplayImage == null)
            {
                // Hata durumunda hemen ��k
                return "konum bilinmiyor";
            }

            // RawImage'in boyutlar�n� al�n (RectTransform, UI uzay�nda)
            var displayRect = m_uiInference.DisplayImage.rectTransform.rect;
            var displayWidth = displayRect.width;
            var displayHeight = displayRect.height;

            // E�er boyutlar 0 ise b�lme hatas� almamak i�in kontrol
            if (displayWidth <= 0 || displayHeight <= 0)
            {
                return "konum bilinmiyor (boyut hatas�)";
            }

            // Normalle�tirilmi� Offset Hesaplama (0 = merkez)
            // CenterX, sol-sa� i�in [-displayWidth/2, +displayWidth/2] aral���ndad�r.
            // CenterY, yukar�-a�a�� i�in [-displayHeight/2, +displayHeight/2] aral���ndad�r.

            // Normalle�tirilmi� X: (-0.5'ten +0.5'e, Sol'dan Sa�'a)
            float normX = centerOffsetPx.x / displayWidth;
            // Normalle�tirilmi� Y: (-0.5'ten +0.5'e, A�a��'dan Yukar�'ya)
            // NOT: UI'da (0,0) merkezdir. Positive Y yukar�d�r, Negative Y a�a��d�r.
            float normY = centerOffsetPx.y / displayHeight;

            // 1. Yatay (Sol/Sa�) Konum Hesaplama
            string horizontalPos = "merkezde";
            // Sol = Negatif X
            if (normX < -HorizontalThreshold)
            {
                horizontalPos = "solda";
            }
            // Sa� = Pozitif X
            else if (normX > HorizontalThreshold)
            {
                horizontalPos = "sa�da";
            }

            // 2. Dikey (Yukar�/A�a��) Konum Hesaplama
            string verticalPos = "merkezde";
            // A�a�� = Negatif Y
            if (normY < -VerticalThreshold)
            {
                verticalPos = "yukar�da";
            }
            // Yukar� = Pozitif Y
            else if (normY > VerticalThreshold)
            {
                verticalPos = "a�a��da";
            }

            // 3. Derinlik (Yak�n/Uzak) Konum Hesaplama (3D)
            string depthPos = "";

            if (worldPosition.HasValue)
            {
                // Nesnenin kameradan uzakl���n� hesaplay�n
                float distance = Vector3.Distance(m_mainCamera.transform.position, worldPosition.Value);

                if (distance < NearDistanceThreshold)
                {
                    depthPos = "yak�nda"; // 1 metreden yak�n
                }
                else if (distance > FarDistanceThreshold)
                {
                    depthPos = "uzakta"; // 3 metreden uzak
                }
            }
            else
            {
                depthPos = "derinli�i bilinmiyor";
            }

            // Sonu� dizisini birle�tirin (�nceki yan�tta oldu�u gibi)
            List<string> parts = new List<string>();

            // Merkezde ve merkezde (yatay ve dikey) ise sadece "ekran�n merkezinde" de diyebiliriz.
            if (horizontalPos == "merkezde" && verticalPos == "merkezde")
            {
                parts.Add("ekran�n merkezinde");
            }
            else
            {
                // Sadece "merkezde" (horizontal) veya "merkezde" (vertical) ise ekleyelim.
                if (horizontalPos != "merkezde") parts.Add(horizontalPos);
                if (verticalPos != "merkezde") parts.Add(verticalPos);
            }

            // Derinlik bilgisini ekleyelim (e�er varsa)
            if (!string.IsNullOrEmpty(depthPos) && depthPos != "derinli�i bilinmiyor")
            {
                parts.Add(depthPos);
            }

            if (parts.Count == 0)
            {
                // E�er hepsi merkezde ve derinli�i de orta mesafede ise
                return "orta mesafede";
            }

            // T�m par�alar� virg�l ve "ve" ile birle�tirin.
            string result = "";
            for (int i = 0; i < parts.Count; i++)
            {
                if (i > 0)
                {
                    if (i == parts.Count - 1)
                    {
                        result += " ve ";
                    }
                    else
                    {
                        result += ", ";
                    }
                }
                result += parts[i];
            }

            return result;
        }



        #endregion

        #region Marker Functions
        /// <summary>
        /// Clean 3d markers when the tracking space is re-centered.
        /// </summary>
        private void CleanMarkersCallBack()
        {
            foreach (var e in m_spwanedEntities)
            {
                Destroy(e, 0.1f);
            }
            m_spwanedEntities.Clear();
            OnObjectsIdentified?.Invoke(-1);
        }
        /// <summary>
        /// Spwan 3d markers for the detected objects
        /// </summary>
        private void SpwanCurrentDetectedObjects()
        {
            var count = 0;
            foreach (var box in m_uiInference.BoxDrawn)
            {
                if (PlaceMarkerUsingEnvironmentRaycast(box.WorldPos, box.ClassName))
                {
                    count++;
                }
            }
            if (count > 0)
            {
                // Play sound if a new marker is placed.
                m_placeSound.Play();
            }
            OnObjectsIdentified?.Invoke(count);
        }

        /// <summary>
        /// Place a marker using the environment raycast
        /// </summary>
        private bool PlaceMarkerUsingEnvironmentRaycast(Vector3? position, string className)
        {
            // Check if the position is valid
            if (!position.HasValue)
            {
                return false;
            }

            // Check if you spanwed the same object before
            var existMarker = false;
            foreach (var e in m_spwanedEntities)
            {
                var markerClass = e.GetComponent<DetectionSpawnMarkerAnim>();
                if (markerClass)
                {
                    var dist = Vector3.Distance(e.transform.position, position.Value);
                    if (dist < m_spawnDistance && markerClass.GetYoloClassName() == className)
                    {
                        existMarker = true;
                        break;
                    }
                }
            }

            if (!existMarker)
            {
                // spawn a visual marker
                var eMarker = Instantiate(m_spwanMarker);
                m_spwanedEntities.Add(eMarker);

                // Update marker transform with the real world transform
                eMarker.transform.SetPositionAndRotation(position.Value, Quaternion.identity);
                eMarker.GetComponent<DetectionSpawnMarkerAnim>().SetYoloClassName(className);
            }

            return !existMarker;
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// Pause the detection logic when the pause menu is active
        /// </summary>
        public void OnPause(bool pause)
        {
            m_isPaused = pause;
        }
        #endregion
    }
}
