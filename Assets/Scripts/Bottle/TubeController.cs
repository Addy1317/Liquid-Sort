using System.Collections.Generic;
using UnityEngine;

namespace SlowpokeStudio.Tube
{
    [ExecuteAlways]
    public class TubeController : MonoBehaviour
    {
        [Header("Renderer / Shader")]
        [SerializeField] private SpriteRenderer bottleMaskSR;

        [Tooltip("Material property names expected by the liquid shader.")]
        [SerializeField] private string propC1 = "_C1";
        [SerializeField] private string propC2 = "_C2";
        [SerializeField] private string propC3 = "_C3";
        [SerializeField] private string propC4 = "_C4";
        [SerializeField] private string propFill = "_FillAmount";
        [SerializeField] private string propSarm = "_SARM";

        [Header("Fill Steps (index = #layers)")]
        [Tooltip("fillAmountSteps[0..4] -> how full the bottle looks for 0..4 layers.")]
        [SerializeField] private float[] fillAmountSteps = new float[5] { 0f, 0.25f, 0.5f, 0.75f, 1f };

        [Header("Initial Colors (bottom -> top)")]
        [Tooltip("Bottom at index 0, top at last. Max 4 entries.")]
        [SerializeField] private List<Color> initialColors = new List<Color>(4);

        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Vector3 gizmoOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField] private Vector2 gizmoSlotSize = new Vector2(0.25f, 0.12f);
        [SerializeField] private float gizmoSlotGap = 0.02f;

        public const int Capacity = 4;

        // Runtime state: bottom -> top
        private readonly List<Color> _layers = new List<Color>(Capacity);

        // Reuse a property block so we never mutate shared materials.
        private MaterialPropertyBlock _mpb;

        #region Public Read API
        public int Count => _layers.Count;
        public bool IsEmpty => _layers.Count == 0;
        public bool IsFull => _layers.Count >= Capacity;
        public Color TopColor => IsEmpty ? Color.clear : _layers[_layers.Count - 1];
        public int TopRunLength
        {
            get
            {
                if (IsEmpty) return 0;
                var top = TopColor;
                int run = 1;
                for (int i = _layers.Count - 2; i >= 0; i--)
                {
                    if (_layers[i] == top) run++;
                    else break;
                }
                return run;
            }
        }
        #endregion

        #region Unity
        private void Awake()
        {
            EnsureSRAndMPB();
            if (Application.isPlaying)
            {
                ResetFromInitial();
            }
            PushToShader(); // keep editor/runtime in sync
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Clamp initial list in editor, don’t mutate runtime list while playing
            if (!Application.isPlaying)
            {
                if (initialColors.Count > Capacity)
                    initialColors.RemoveRange(Capacity, initialColors.Count - Capacity);

                EnsureSRAndMPB();
                // Preview initial colors directly in editor
                PushToShaderPreview(initialColors);
            }
        }
#endif

        private void Reset()
        {
            // best-effort: try find a SpriteRenderer on this GO if not assigned
            if (!bottleMaskSR) bottleMaskSR = GetComponent<SpriteRenderer>();
        }
        #endregion

        #region Public Mutations (used by PourManager later)
        /// <summary>Can this bottle receive a run of the given color?</summary>
        public bool CanReceive(Color c)
        {
            if (IsFull) return false;
            if (IsEmpty) return true;
            return TopColor.Equals(c);
        }

        /// <summary>
        /// Remove up to maxCount layers from the top, but only of the same color as current top.
        /// Returns the removed layers in bottom->top order.
        /// </summary>
        public List<Color> TakeTopRun(int maxCount)
        {
            var taken = new List<Color>(4);
            if (IsEmpty || maxCount <= 0) return taken;

            int run = Mathf.Min(TopRunLength, maxCount);
            int start = _layers.Count - run;
            for (int i = 0; i < run; i++)
                taken.Add(_layers[start + i]); // preserves bottom->top order for the run

            _layers.RemoveRange(start, run);
            Debug.Log($"[BottleController:{name}] TakeTopRun run={run}, newCount={_layers.Count}");

            PushToShader();
            return taken;
        }

        /// <summary>
        /// Add layers on top (expects they are same color or already validated externally).
        /// Accepts either bottom->top or top->bottom; we’ll normalize to bottom->top append.
        /// </summary>
        public void AddLayers(IReadOnlyList<Color> incoming, bool incomingIsBottomToTop = true)
        {
            if (incoming == null || incoming.Count == 0) return;

            // Normalize order to bottom->top
            if (!incomingIsBottomToTop)
            {
                // Reverse copy
                for (int i = incoming.Count - 1; i >= 0; i--)
                    _layers.Add(incoming[i]);
            }
            else
            {
                for (int i = 0; i < incoming.Count; i++)
                    _layers.Add(incoming[i]);
            }

            if (_layers.Count > Capacity)
            {
                Debug.LogWarning($"[BottleController:{name}] Over capacity! Clamping from {_layers.Count} to {Capacity}.");
                _layers.RemoveRange(Capacity, _layers.Count - Capacity);
            }

            Debug.Log($"[BottleController:{name}] AddLayers +{incoming.Count}, newCount={_layers.Count}");
            PushToShader();
        }
        #endregion

        #region Initialization & Shader Sync
        public void ResetFromInitial()
        {
            _layers.Clear();
            for (int i = 0; i < Mathf.Min(initialColors.Count, Capacity); i++)
                _layers.Add(initialColors[i]);

            Debug.Log($"[BottleController:{name}] ResetFromInitial count={_layers.Count}");
            PushToShader();
        }

        private void EnsureSRAndMPB()
        {
            if (!bottleMaskSR) bottleMaskSR = GetComponent<SpriteRenderer>();
            _mpb ??= new MaterialPropertyBlock();
        }

        private void PushToShader()
        {
            if (!bottleMaskSR) return;
            EnsureSRAndMPB();

            // Colors: bottom -> top, pad with clear
            var c1 = _layers.Count > 0 ? _layers[0] : Color.clear;
            var c2 = _layers.Count > 1 ? _layers[1] : Color.clear;
            var c3 = _layers.Count > 2 ? _layers[2] : Color.clear;
            var c4 = _layers.Count > 3 ? _layers[3] : Color.clear;

            bottleMaskSR.GetPropertyBlock(_mpb);
            _mpb.SetColor(propC1, c1);
            _mpb.SetColor(propC2, c2);
            _mpb.SetColor(propC3, c3);
            _mpb.SetColor(propC4, c4);

            float fill = Mathf.Clamp01(fillAmountSteps[Mathf.Clamp(_layers.Count, 0, 4)]);
            _mpb.SetFloat(propFill, fill);

            // Idle SARM (PourManager will animate this later)
            _mpb.SetFloat(propSarm, 0f);

            bottleMaskSR.SetPropertyBlock(_mpb);
        }

#if UNITY_EDITOR
        // Editor preview for initialColors (so you can see changes without Enter Play Mode)
        private void PushToShaderPreview(List<Color> preview)
        {
            if (!bottleMaskSR) return;
            EnsureSRAndMPB();

            var c1 = preview.Count > 0 ? preview[0] : Color.clear;
            var c2 = preview.Count > 1 ? preview[1] : Color.clear;
            var c3 = preview.Count > 2 ? preview[2] : Color.clear;
            var c4 = preview.Count > 3 ? preview[3] : Color.clear;

            bottleMaskSR.GetPropertyBlock(_mpb);
            _mpb.SetColor(propC1, c1);
            _mpb.SetColor(propC2, c2);
            _mpb.SetColor(propC3, c3);
            _mpb.SetColor(propC4, c4);
            float fill = Mathf.Clamp01(fillAmountSteps[Mathf.Clamp(preview.Count, 0, 4)]);
            _mpb.SetFloat(propFill, fill);
            _mpb.SetFloat(propSarm, 0f);
            bottleMaskSR.SetPropertyBlock(_mpb);
        }
#endif
        #endregion

        #region Gizmos
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            // Draw 4 slots stacked above the bottle
            var basePos = transform.position + gizmoOffset;

            // Outline
            Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
            Vector3 outlineSize = new Vector3(gizmoSlotSize.x, gizmoSlotSize.y * 4 + gizmoSlotGap * 3, 0.01f);
            Gizmos.DrawWireCube(basePos + new Vector3(0, (outlineSize.y - gizmoSlotSize.y) * 0.5f, 0), outlineSize);

            // Draw from bottom (slot 0) up to top (slot 3)
            for (int i = 0; i < Capacity; i++)
            {
                var slotPos = basePos + new Vector3(0, i * (gizmoSlotSize.y + gizmoSlotGap), 0);
                Color col = (i < _layers.Count) ? _layers[i] : new Color(1f, 1f, 1f, 0.05f);

                Gizmos.color = col.a < 0.01f ? new Color(1, 1, 1, 0.05f) : col;
                Gizmos.DrawCube(slotPos, new Vector3(gizmoSlotSize.x, gizmoSlotSize.y, 0.01f));

                // Border for clarity
                Gizmos.color = new Color(0, 0, 0, 0.35f);
                Gizmos.DrawWireCube(slotPos, new Vector3(gizmoSlotSize.x, gizmoSlotSize.y, 0.01f));
            }
        }
        #endregion
    }
}
