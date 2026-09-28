using Cossacks2Bridge.Core;
using UnityEngine;
using Cossacks2Bridge.UnityAdapters.AddProfile;

namespace Cossacks2Bridge.UnityAdapters.Renderers
{
    // Рисуем AddProfile через OptionsRenderer, но без Screen.resolutions
    public sealed class NewPlayerRenderer
    {
        private readonly OptionsRenderer _inner = new OptionsRenderer();

        public void Render(UiDesk desk, CoreFileSystem fs, BaseUiRenderer.RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            // копия опций, чтобы не затронуть остальные экраны
            var localOpt = new BaseUiRenderer.RenderOptions
            {
                FontResourcePath = opt.FontResourcePath,
                FontSize = opt.FontSize,
                NormalColor = opt.NormalColor,
                HoverColor = opt.HoverColor,
                DisabledColor = opt.DisabledColor,
                CanvasScaleMode = opt.CanvasScaleMode,
                ReferenceResolution = opt.ReferenceResolution,
                VerboseLogs = opt.VerboseLogs,
                DrawDebugOutline = opt.DrawDebugOutline,

                FillResolutionCombos = false
            };

            _inner.Render(desk, fs, localOpt, sink, loc);

            // AddProfile extras: commander portraits + description + scrollers.
            //
            // AP1 lifecycle fix:
            // OptionsRenderer deliberately reuses C2_OptionsCanvas and destroys only
            // its children on every rebuild.  AddProfileCommanderController lives on
            // the canvas itself, therefore the old component survived a close/open
            // cycle while all RectTransform references cached by Start() pointed to
            // the destroyed previous XML controls.  Start() is not called twice on
            // the same MonoBehaviour, so the second AddProfile opening was left in
            // the raw/intermediate XML state (test Description text and the native
            // un-restyled portrait scrollbar).
            //
            // The original StartDS(AddProfile) lifecycle creates a fresh dialog/action
            // instance on each opening.  Mirror that lifecycle here: retire the stale
            // controller after the XML tree has been rebuilt and attach a fresh one.
            var canvas = UnityEngine.GameObject.Find("C2_OptionsCanvas");
            if (canvas != null)
            {
                var stale = canvas.GetComponent<Cossacks2Bridge.UnityAdapters.AddProfile.AddProfileCommanderController>();
                string staleId = stale != null ? stale.GetEntityId().ToString() : "none";

                if (stale != null)
                {
                    // Disable immediately so OnDisable unsubscribes from static runtime
                    // events and the stale component cannot run LateUpdate against the
                    // newly rebuilt XML children. Destroy itself may remain deferred to
                    // the end of the frame, which is safe because it is disabled.
                    stale.enabled = false;
                    UnityEngine.Object.Destroy(stale);
                }

                var fresh = canvas.AddComponent<Cossacks2Bridge.UnityAdapters.AddProfile.AddProfileCommanderController>();
                fresh.Init(fs, loc);

                Debug.Log($"[C2:ADDPROFILE LIFECYCLE AP1] frame={Time.frameCount} " +
                          $"canvasChildren={canvas.transform.childCount} staleController={staleId} " +
                          $"freshController={fresh.GetEntityId()} action=fresh_rebind");
            }
        }
    }
}