using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Liv.Lck
{
    public class LckHideObjectFromCamera : MonoBehaviour
    {
        [SerializeField] 
        private Camera _targetCamera;
        [SerializeField]
        private string _hiddenLayerName = "HideInRecording";

        private int _hiddenLayer;
        private int _originalLayer;
        private bool _dirty;

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
            RenderPipelineManager.endCameraRendering += EndCameraRendering;

            _hiddenLayer = LayerMask.NameToLayer(_hiddenLayerName);
            _originalLayer = gameObject.layer;

            HideCanvases(transform);
        }

        private void EndCameraRendering(ScriptableRenderContext arg1, Camera cameraBeingRendered)
        {
            if(cameraBeingRendered != _targetCamera)
                return;
            
            if(!_dirty)
                return;
            
            FrameCleanup();
        }

        void HideCanvases(Transform parent)
        {
            foreach (Transform child in parent)
            {
                var canvas = child.GetComponent<Canvas>();
                if (canvas)
                {
                    child.gameObject.layer = _hiddenLayer;
                }
                
                var tmp = child.GetComponent<TextMeshPro>();
                if (tmp)
                {
                    child.gameObject.layer = _hiddenLayer;
                }
                
                HideCanvases(child);
            }
        }

        private void BeginCameraRendering(ScriptableRenderContext scriptableRenderContext, Camera cameraBeingRendered)
        {
            if (cameraBeingRendered == _targetCamera)
            {
                SetLayerRecursively(gameObject, _hiddenLayer);
                _dirty = true;
            }
        }

        private void FrameCleanup()
        {
            if(!_dirty)
                return;
            
            SetLayerRecursively(gameObject, _originalLayer);
            _dirty = false;
        }

        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) 
                return;

            var canvas = obj.GetComponent<Canvas>();
            if (!canvas)
            {
                obj.layer = newLayer;
            }
            
            foreach (Transform child in obj.transform)
            {
                if (child != null)
                {
                    SetLayerRecursively(child.gameObject, newLayer);
                }
            }
        }
        
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            RenderPipelineManager.endCameraRendering += EndCameraRendering;
        }
    }
}