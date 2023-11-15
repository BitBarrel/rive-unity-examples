using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

using LoadAction = UnityEngine.Rendering.RenderBufferLoadAction;
using StoreAction = UnityEngine.Rendering.RenderBufferStoreAction;

namespace Rive
{
    [ExecuteInEditMode]
    public class RiveTexture : MonoBehaviour
    {
        public Rive.Asset asset;
        public RenderTexture renderTexture;
        public Fit fit = Fit.contain;
        public Alignment alignment = Alignment.center;

        private RenderQueue m_renderQueue;
        private CommandBuffer m_commandBuffer;

        private Rive.File m_file;
        private Artboard m_artboard;
        private StateMachine m_stateMachine;

        private Camera m_camera;

        private void Start()
        {
            m_renderQueue = new RenderQueue(renderTexture);
            if (asset != null)
            {
                m_file = Rive.File.load(asset);
                m_artboard = m_file.artboard(0);
                m_stateMachine = m_artboard?.stateMachine();
            }

            if (m_artboard != null && renderTexture != null)
            {
                m_renderQueue.align(fit, alignment, m_artboard);
                m_renderQueue.draw(m_artboard);

                m_commandBuffer = new CommandBuffer();
                m_renderQueue.toCommandBuffer();
                m_commandBuffer.SetRenderTarget(renderTexture);
                m_commandBuffer.ClearRenderTarget(true, true, UnityEngine.Color.clear, 0.0f);
                m_renderQueue.addToCommandBuffer(m_commandBuffer);
                m_camera = Camera.main;
                if (m_camera != null)
                {
                    Camera.main.AddCommandBuffer(CameraEvent.AfterEverything, m_commandBuffer);
                }
            }
        }

        private void Update()
        {

            HitTesting();

            if (m_stateMachine != null)
            {
                m_stateMachine.advance(Time.deltaTime);
            }
        }

        bool m_wasMouseDown = false;
        private Vector2 m_lastMousePosition;

        void HitTesting()
        {
            Camera camera = Camera.main;

            if (camera == null || renderTexture == null || m_artboard == null) return;

            if (!Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
                return;

            Renderer rend = hit.transform.GetComponent<Renderer>();
            MeshCollider meshCollider = hit.collider as MeshCollider;

            if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.mainTexture == null || meshCollider == null)
                return;

            Vector2 pixelUV = hit.textureCoord;

            pixelUV.x *= renderTexture.width;
            pixelUV.y *= renderTexture.height;

            Vector3 mousePos = camera.ScreenToViewportPoint(Input.mousePosition);
            Vector2 mouseRiveScreenPos = new(mousePos.x * camera.pixelWidth, (1 - mousePos.y) * camera.pixelHeight);

            if (m_lastMousePosition != mouseRiveScreenPos || transform.hasChanged)
            {
                Vector2 local = m_artboard.localCoordinate(pixelUV, new Rect(0, 0, renderTexture.width, renderTexture.height), fit, alignment);
                m_stateMachine?.pointerMove(local);
                m_lastMousePosition = mouseRiveScreenPos;
            }
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 local = m_artboard.localCoordinate(pixelUV, new Rect(0, 0, renderTexture.width, renderTexture.height), fit, alignment);
                m_stateMachine?.pointerDown(local);
                m_wasMouseDown = true;
            }
            else if (m_wasMouseDown)
            {
                m_wasMouseDown = false; Vector2 local = m_artboard.localCoordinate(mouseRiveScreenPos, new Rect(0, 0, renderTexture.width, renderTexture.height), fit, alignment);
                m_stateMachine?.pointerUp(local);
            }
        }

        private void OnDisable()
        {
            if (m_camera != null && m_commandBuffer != null)
            {
                m_camera.RemoveCommandBuffer(CameraEvent.AfterEverything, m_commandBuffer);
            }
        }
    }
}
