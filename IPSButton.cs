using System.ComponentModel.Composition;
using WinTak.Common.Services;
using WinTak.Framework.Docking;
using WinTak.Framework.Tools;
using WinTak.Framework.Tools.Attributes;

namespace ipswintakplugin
{
    [Button("Template_IPSButton", "IPS Plugin",
    Tab = "IPS XP",
    TabGroup = "IPS Tools",
    LargeImage = "pack://application:,,,/ipswintakplugin;component/assets/ic_launcher.svg",
    SmallImage = "pack://application:,,,/ipswintakplugin;component/assets/ic_launcher_24x24.png")]
    internal class IPSButton: Button
    {
        private IDockingManager _dockingManager;
        private IMapObjectRenderer _renderer;
        [ImportingConstructor]
        public IPSButton(IDockingManager dockingManager, IMapObjectRenderer mapObjectRenderer)
        {
            _dockingManager = dockingManager;
            _renderer = mapObjectRenderer;
        }
        protected override void OnClick()
        {
            base.OnClick();

            var pane = _dockingManager.GetDockPane(IPSDockPane.ID);
            pane?.Activate();
        }
    }
}
