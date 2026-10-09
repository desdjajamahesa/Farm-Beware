using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Centralized LIFO Modal Stack Manager guaranteeing single-window focus,
    /// strict LIFO dismissal, and race-free ESC key routing.
    /// </summary>
    public class ModalStackManager : MonoBehaviour, IModalStackService
    {
        public static ModalStackManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ModalStackManager>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        var go = new GameObject("ModalStackManager");
                        _instance = go.AddComponent<ModalStackManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }
        private static ModalStackManager _instance;

        private readonly List<IModalWindow> _stack = new List<IModalWindow>();

        public int OpenModalCount => _stack.Count;
        public bool HasActiveModal => _stack.Count > 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            ServiceLocator.Register<IModalStackService>(this);
        }

        private void Update()
        {
            bool escapePressed = false;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                escapePressed = true;
            }

            if (escapePressed && HasActiveModal)
            {
                int lastIndex = _stack.Count - 1;
                var top = _stack[lastIndex];
                if (top != null && top.CanDismissWithEscape)
                {
                    Pop();
                }
            }
        }

        /// <summary>
        /// Registers and pushes an opened modal to the top of the LIFO stack.
        /// </summary>
        public void Push(IModalWindow modal)
        {
            if (modal == null) return;

            // Remove existing occurrence if already present
            _stack.Remove(modal);
            _stack.Add(modal);

            UIModalHelper.IsSaveUIOpen = true;
        }

        /// <summary>
        /// Pops and closes the topmost active modal window.
        /// </summary>
        public void Pop()
        {
            if (_stack.Count == 0) return;

            int lastIndex = _stack.Count - 1;
            var top = _stack[lastIndex];
            _stack.RemoveAt(lastIndex);

            UIModalHelper.LastFrameUIPanelClosed = Time.frameCount;

            if (top != null && top.IsOpen)
            {
                top.CloseModal();
            }

            if (_stack.Count == 0)
            {
                UIModalHelper.IsSaveUIOpen = false;
            }
        }

        /// <summary>
        /// Removes a specific modal window from the stack (e.g. when closed via UI button).
        /// </summary>
        public void PopSpecific(IModalWindow modal)
        {
            if (modal == null) return;

            _stack.Remove(modal);
            UIModalHelper.LastFrameUIPanelClosed = Time.frameCount;

            if (_stack.Count == 0)
            {
                UIModalHelper.IsSaveUIOpen = false;
            }
        }

        /// <summary>
        /// Dismisses all currently open modals in reverse order.
        /// </summary>
        public void CloseAll()
        {
            while (_stack.Count > 0)
            {
                Pop();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                ServiceLocator.Unregister<IModalStackService>();
                _instance = null;
            }
        }
    }
}
