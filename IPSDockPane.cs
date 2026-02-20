using ipswintakplugin.Common;
using ipswintakplugin.Notifications;
using ipswintakplugin.Properties;
using MapEngine.Interop.Util;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Windows.Web.Http;
using WinTak.Alerts;
using WinTak.Common.Coords;
using WinTak.Common.CoT;
using WinTak.Common.Geofence;
using WinTak.Common.Location;
using WinTak.Common.Messaging;
using WinTak.Common.Preferences;
using WinTak.Common.Services;
using WinTak.Common.Utils;
using WinTak.CursorOnTarget;
using WinTak.CursorOnTarget.Placement.DockPanes;
using WinTak.CursorOnTarget.Services;
using WinTak.Display;
using WinTak.Display.Controls;
using WinTak.Framework;
using WinTak.Framework.Docking;
using WinTak.Framework.Docking.Attributes;
using WinTak.Framework.Messaging;
using WinTak.Framework.Notifications;
using WinTak.Graphics;
using WinTak.Graphics.Map;
using WinTak.Location.Providers;
using WinTak.Location.Services;
using WinTak.Mapping;
using WinTak.Mapping.Services;
using WinTak.MissionPackages;
using WinTak.Net.Contacts;
using WinTak.Overlays.Services;
using WinTak.Overlays.ViewModels;
using WinTak.UI;
using WinTak.UI.Themes;


namespace ipswintakplugin
{
    [DockPane(ID, "Template", Content = typeof(IPSView), DockLocation = DockLocation.Left, PreferredWidth = 300)]
    internal class IPSDockPane : DockPane
    {
        internal const string ID = "IPS_IPSDockPane";
        internal const string TAG = "IPSDockPane";
        private readonly ICotMessageSender _cotMessageSender;
        private readonly IContactService _contactList;
        private readonly IImageStore _imageStore;
        private IMissionPackageService _missionPackageService;

        /* Common */
        // public ILogger _logger; // Or use the Log.x like ATAK
        private readonly IMessageHub _messageHub;
        private IUnitDisplayPreferences _unitDisplayPreferences;
        private IStatusIndicator _statusIndicator;
        private ILocationPreferences _locationPreferences;
        private IIPSLocationPreferences _IPSLocationPreferences;
        private ILocationProvider _locationProvider;
        public IDevicePreferences _devicePreferences;
        public ILocationService _locationService;

        //public ARenderableListLayer _renderableListLayer;
        //public GLMapView _glMapView;

        private TaskCompletionSource<MapItem> _selectionTcs;

        public IDockingManager _dockingManager;
        public ICoTManager _coTManager;
        public IElevationManager _elevationManager;
        public IMapGroupManager _mapGroupManager;
        public IMapObjectRenderer _mapObjectRenderer;
        private readonly ImageSource _customMarkerHWImageSource;
        private CompositeMapItem _customMarkerCompositeMapItem;
        private MapObjectItem _customMarker;
        private MapGroup _mapGroup;
        private WheelMenuItem _wheelMenuItem;
        private readonly IPrecisionMoveService _precisionMoveService;
        public DockPaneAttribute DockPaneAttribute { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly IMapViewController _mapViewController;

        public IAlertProvider _alertProvider { get; private set; }
        public IAlert _alert { get; private set; }
        public IGeofenceManager _geofenceManager { get; private set; }
        public IMapObjectItemManager _mapObjectItemManager { get; private set; }

        private string cotGuidGenerate;
        public string callsignName;
        public string inputTextMsg;
        public string CallSignName
        {
            get
            {
                return this.callsignName;
            }
            //set => SetAndSubscribeProperty(ref testInputMesg, value);
            //base.SetProperty(ref testInput)
            set
            {
                if (base.SetProperty(ref callsignName, value, nameof(CallSignName)))
                {
                    Log.i(TAG, "Save/Update the value inside : " + callsignName + "'");
                }
            }
        }
        public string InputTextMsg
        {
            get
            {
                return this.inputTextMsg;
            }
            //set => SetAndSubscribeProperty(ref testInputMesg, value);
            //base.SetProperty(ref testInput)
            set
            {
                if (base.SetProperty(ref inputTextMsg, value, nameof(InputTextMsg)))
                {
                    Log.i(TAG, "Save/Update the value inside : " + inputTextMsg + "'");
                }
            }
        }
        /****** CoT Manager ***** */
        ICotMessageReceiver _cotMessageReceiver;

        /* ***** Marker Manipulation ***** */
        public ICommand SpecialMarkerBtn { get; private set; }
        public ICommand AddStreamBtn { get; private set; }
        public ICommand ItemInspectBtn { get; private set; }

        /* ***** Notification Examples ***** */
        public ICommand GetCurrentNotificationsBtn { get; private set; }
        public ICommand FakeContentProviderBtn { get; private set; }
        public ICommand NotificationSpammerBtn { get; private set; }
        public ICommand NotificationWithOptionsBtn { get; private set; }
        public ICommand NotificationToWinTakToastBtn { get; private set; }
        public ICommand NotificationToWindowsBtn { get; private set; }

        public INotificationLog _notificationLog;

        /* ***** Web */
        public ICommand WebViewBtn { get; private set; }
        public ICommand WebRecordViewBtn { get; private set; }

        /* ****** QR */
        public ICommand ScanAnimatedQrBtn { get; private set; }

        /* ***** Plugin Template Duplicate (From WinTAK-Documentation) ***** */
        public ICommand IncreaseCounterBtn { get; private set; }
        public ICommand WhiteHouseCoTBtn { get; private set; }

        private int _counter;
        private double _mapFunctionLat;
        private double _mapFunctionLon;
        private bool _mapFunctionIsActivate;
        private bool _isCotInspectionActive = false;

        // -- ----- ----- ----- ----- CONSTRUCTOR ----- ----- ----- ----- -- //
        [ImportingConstructor] // this import provide the capability to get WinTAK exposed Interfaces
        public IPSDockPane(
            ICommunicationService communicationService,
            ICoTManager coTManager,
            ICotMessageReceiver cotMessageReceiver,
            ICotMessageSender cotMessageSender,
            IDevicePreferences devicePreferences,
            IDockingManager dockingManager,
            IElevationManager elevationManager,
            IGeocoderService geocoderService,
            ILogger logger,
            ILocationService locationService,
            /* IMapObjectFinderService mapObjectFinderService, */
            IMapGroupManager mapGroupManager,
            IMapObjectItemManager mapObjectItemManager,
            IMapObjectRenderer mapObjectRenderer,
            IMessageHub messageHub,
            INotificationLog notificationLog,
            IMapViewController mapViewController,

            IGeofenceManager geofenceManager,

            IPrecisionMoveService precisionMoveMarking,

            IContactService contactService,
            IImageStore imageStore,
            IMissionPackageService missionPackageService,
            IUnitDisplayPreferences unitDisplayPreferences,
            IStatusIndicator statusIndicator,
            ILocationPreferences locationPreferences
            //ARenderableListLayer renderableListLayer
            //GLMapView gLMapView
            //IIPSLocationPreferences IPSLocationPreferences
            )
        {

            // Test
            _cotMessageSender = cotMessageSender;
            _contactList = contactService;
            _imageStore = imageStore;
            _missionPackageService = missionPackageService;
            _communicationService = communicationService;
            /* Interface link */
            //_logger = logger;
            _messageHub = messageHub;
            _dockingManager = dockingManager;
            _locationService = locationService;
            _locationService.PositionChanged += OnPositionChanged;

            Log.i(TAG, "_locationService.GetGpsHeading()       : " + _locationService.GetGpsHeading());
            Log.i(TAG, "_locationService.GetGpsMarker()        : " + _locationService.GetGpsMarker());
            Log.i(TAG, "_locationService.GetGpsObject()        : " + _locationService.GetGpsObject());
            Log.i(TAG, "_locationService.GetGpsPosition()      : " + _locationService.GetGpsPosition());
            Log.i(TAG, "_locationService.GetGpsSpeed()         : " + _locationService.GetGpsSpeed());
            Log.i(TAG, "_locationService.GetHashCode()         : " + _locationService.GetHashCode());
            Log.i(TAG, "_locationService.GetPositionDocument() : " + _locationService.GetPositionDocument());
            Log.i(TAG, "_locationService.GetSelfCotEvent()     : " + _locationService.GetSelfCotEvent());
            Log.i(TAG, "_locationService.GetType()             : " + _locationService.GetType());
            //Log.i(TAG, "_locationService.ConnectionStatus      : " + _locationService.ConnectionStatus);
            //Log.i(TAG, "_locationService.HasConnections        : " + _locationService.HasConnections);
            //Log.i(TAG, "_locationService.IsSimulatedGps        : " + _locationService.IsSimulatedGps);

            _unitDisplayPreferences = unitDisplayPreferences;
            _statusIndicator = statusIndicator;
            _locationPreferences = locationPreferences;
            //_locationProvider = locationProvider;
            //_IPSLocationPreferences = IPSLocationPreferences;

            _devicePreferences = devicePreferences;
            _notificationLog = notificationLog;
            _coTManager = coTManager;
            _elevationManager = elevationManager;
            _mapGroupManager = mapGroupManager;
            _mapObjectRenderer = mapObjectRenderer;
            _precisionMoveService = precisionMoveMarking;
            //_renderableListLayer = renderableListLayer;
            //_glMapView = gLMapView;
            _customMarkerHWImageSource = new BitmapImage(new Uri("pack://application:,,,/ipswintakplugin;component/assets/brand_cthulhu.png"));
            _customMarkerCompositeMapItem = new CompositeMapItem();
            Log.i(TAG, "The customMarkerCompositeMapItem : " + this._customMarkerCompositeMapItem.GetUid());

            _mapObjectItemManager = mapObjectItemManager;
            ICollection<MapObjectItem> rootItems = mapObjectItemManager.RootItems;
            if (rootItems != null)
            {
                _customMarker = new LegacyMapObjectItem(Resources.btnRecyclerViewName, new BitmapImage(new Uri("pack://application:,,,/ipswintakplugin;component/assets/brand_cthulhu.png")))
                {
                    Visible = true,
                    Selectable = false,
                    Id = "IPS CustomMarker"
                };
                rootItems.Add(_customMarker);
            }
            _mapGroup = mapGroupManager.GetOrCreateMapGroup("IPSMapGroup");
            _customMarkerCompositeMapItem.Disposing += CustomHWOnDisposing;

            this.CallSignName = _devicePreferences.Callsign;
            this.InputTextMsg = "A default text message from constructor.";
            Log.i(TAG, "" + locationService.GetGpsPosition());
            foreach (MapObjectItem mapObjectItem in rootItems)
            {
                if (mapObjectItem.Text == "Geo Fences")
                {
                    Log.i(TAG, "MapObjectItem : " + mapObjectItem.ToString());
                    Log.i(TAG, "Text : " + mapObjectItem.Text);
                    Log.i(TAG, "We have the Geo Fences Overlay");
                    int itemCount = mapObjectItem.GetSubItemCount(); // put 0 becasue does not point to the Geo Fences icons but to the sub items.
                                                                     // We nee to list the items inside of it/
                    Log.i(TAG, "Number of items : " + itemCount.ToString());
                    Log.i(TAG, "Id : " + mapObjectItem.Id);
                    int childCount = mapObjectItem.ChildCount;
                    Log.i(TAG, "ChildCount : " + childCount.ToString());
                    //MapItem mapItems = mapObjectItem.MapItem;
                    //Log.i(TAG, "mapItems : " + mapItems.ToString());

                    ObservableCollection<MapObjectItem> childMapObjectItems = mapObjectItem.Children;
                    foreach (MapObjectItem moi in childMapObjectItems)
                    {
                        Log.i(TAG, "Text : " + moi.Text);
                        Log.i(TAG, "Show Settings : " + moi.ShowSettings);
                        Log.i(TAG, "Id : " + moi.Id);
                        Log.i(TAG, "InViewChildCount : " + moi.InViewChildCount);
                        Log.i(TAG, "Location : " + moi.Location);
                        Log.i(TAG, "Properties : " + moi.Properties);
                        Log.i(TAG, "Position : " + moi.Position);
                        Log.i(TAG, "ShowDetailsCommand : " + moi.ShowDetailsCommand);
                        Log.i(TAG, "SubText : " + moi.SubText);
                        Log.i(TAG, "ToolTip : " + moi.ToolTip);
                        Log.i(TAG, "MapItem : " + moi.MapItem);
                        Log.i(TAG, "ChildCount : " + moi.ChildCount);
                        Log.i(TAG, "Children : " + moi.Children);
                        //Log.i(TAG, " : " + moi.PropertyChanged());
                    }

                }
            }
            foreach (var item in _mapGroupManager.MapItems)
            {
                Log.i(TAG, "MapGroup : " + item.ToString());
                Log.i(TAG, "Text : " + item.Uid);
                Log.i(TAG, "MapItems : " + item.MapItems);
                Log.i(TAG, "ParentMapGroup : " + item.ParentMapGroup);
                Log.i(TAG, "GetCallsign : " + item.GetCallsign());
                Log.i(TAG, "GetUid : " + item.GetUid());
                Log.i(TAG, "Name : " + item.Name);
            }
            _mapViewController = mapViewController;
            this._mapViewController.WheelMenuOpening += MapViewController_WheelMenuOpening;
            // Layout Examples
            //LayoutExamples_Configuration();

            // Marker Manipulation - Special Marker
            var specialMarkerCommand = new ExecutedCommand();
            specialMarkerCommand.Executed += OnDemandExecuted_SpecialMarkerButton;
            SpecialMarkerBtn = specialMarkerCommand;

            var itemInspectCommand = new ExecutedCommand();
            itemInspectCommand.Executed += OnDemandExecuted_ItemInspectBtn;
            ItemInspectBtn = itemInspectCommand;

            // Marker Manipulation - Add Streams
            _cotMessageReceiver = cotMessageReceiver;
            var addStreamCommand = new ExecutedCommand();
            addStreamCommand.Executed += OnDemandExecuted_AddStreamBtn;
            AddStreamBtn = addStreamCommand;

            // Notification Examples
            NotificationExamples_Configuration();

            // WebView
            var webViewCommand = new ExecutedCommand();
            webViewCommand.Executed += OnDemandExecuted_WebViewBtn;
            WebViewBtn = webViewCommand;

            // WebRecordView
            var webRecordViewCommand = new ExecutedCommand();
            webRecordViewCommand.Executed += OnDemandExecuted_WebRecordViewBtn;
            WebRecordViewBtn = webRecordViewCommand;

            // Plugin Template Duplicate (From WinTAK-Documentation)
            var counterButtonCommand = new ExecutedCommand();
            counterButtonCommand.Executed += OnDemandExecuted_IncreaseCounterBtn;
            IncreaseCounterBtn = counterButtonCommand;

            // Plugin Template Duplicate (From WinTAK Reference Documentation)
            var whiteHouseCoTCommand = new ExecutedCommand();
            whiteHouseCoTCommand.Executed += OnDemandExecuted_WhiteHouseCoTBtn;
            WhiteHouseCoTBtn = whiteHouseCoTCommand;

            // QR Code
            var scanAqrCommand = new ExecutedCommand();
            scanAqrCommand.Executed += OnDemandExecuted_ScanAnimatedQrBtn;
            ScanAnimatedQrBtn = scanAqrCommand;

        }

        // --------------------------------------------------------------------
        // Common Method
        // --------------------------------------------------------------------
        private class ExecutedCommand : ICommand
        {
            public event EventHandler CanExecuteChanged;
            public event EventHandler Executed;

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                Executed?.Invoke(this, EventArgs.Empty);
            }

        }

        private void OnWheelMenuForSelection(object sender, MenuPopupEventArgs e)
        {
            // Use the same logic as in your existing WheelMenuOpening code.
            // Here, we attempt to get the clicked item.
            if (e.GetSingleClickedItem(out var clickedObject) != null)
            {
                // Check if it is a MapItem.
                if (clickedObject is MapItem mapItem)
                {
                    Log.i(TAG, "MapItem selected with UID: " + mapItem.GetUid());
                    _selectionTcs.TrySetResult(mapItem);
                }
            }
        }


        private string SaveImageToFile(Bitmap image, string imgName)
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), imgName);
            image.Save(tempFilePath, System.Drawing.Imaging.ImageFormat.Png);
            return tempFilePath;
        }

        protected void OnPropertyChanged(string propertyname)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + propertyname);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CallSignName))
            {
                OnPropertyChanged(e.PropertyName); // Notify property change
            }
        }

        CotDockPane dockPane;
        private ICommunicationService _communicationService;

        private void StatusIndicator_SetSelfMarkerRequest(object sender, EventArgs e)
        {
            Log.i(TAG, "StatusIndicator_SetSelfMarkerRequest : " + sender);
            Log.i(TAG, "StatusIndicator_SetSelfMarkerRequest : " + e);

        }
        private void LocationPanel_ManualPositionRequested(object sender, EventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");
        }
        private void OnPositionChanged(object sender, PositionChangedEventArgs e)
        {
            var newPosition = e.Position;
            var newSpeed = e.Speed;
            var newHeading = e.Heading;
            Log.i(TAG, "OnPositionChanged : newPosition : " + newPosition);
            UpdateSelfPosition(newPosition);
        }

        private void UpdateSelfPosition(TAKEngine.Core.GeoPoint newPosition)
        {
            Log.i(TAG, "UpdateSelfPosition ?");

        }


        private void PlaceIPS_MapClick(object sender, MapMouseEventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");

            /* Variables declaration */
            string cotUid;
            string cotType = "a-f-A";
            string cotName = "HWM"; // IPSMarker
            string cotDetail;

            /* Implementation */
            cotUid = Guid.NewGuid().ToString();
            cotName = _coTManager.CreateCallsign(cotName, CallsignCreationMethod.BasedOnTypeAndDate);
            cotDetail = "<archive /> <_IPS_ title=\"" + cotName + "\" /><precisionlocation altsrc=\"DTED0\" />"; ;
            TAKEngine.Core.GeoPoint geoPoint;
            geoPoint = new TAKEngine.Core.GeoPoint(e.WorldLocation)
            {
                Altitude = _elevationManager.GetElevation(e.WorldLocation),
                AltitudeRef = global::TAKEngine.Core.AltitudeReference.HAE
            };

            if (double.IsNaN(geoPoint.Altitude))
            {
                geoPoint.Altitude = Altitude.UNKNOWN_VALUE;
            }

            cotGuidGenerate = cotUid;
            _coTManager.AddItem(cotUid, cotType, geoPoint, cotName, cotDetail);

            MapViewControl.PopMapEvents();
            Prompt.Clear();
        }
        private void MapObjectAdded(object sender, MapItemEventArgs args)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");
            MapItem mapItem;
            mapItem = args?.MapItem;
            if (mapItem == null || cotGuidGenerate == null || cotGuidGenerate == mapItem.GetUid())
            {
                return;
            }
            base.DispatchAsync(delegate
            {
                if (_dockingManager.GetDockPane(ID) is IPSDockPane IPSDockPane)
                {
                    WinTak.Graphics.MapMarker mapMarker;
                    mapMarker = mapItem.GetMapMarker();
                    if (mapMarker != null)
                    {
                        // IPSDockPane.SetMarker(mapMarker);
                        mapItem.Properties.TryGetValue("IPSMapItem", out var value);
                        if (value != null)
                        {
                            ((MapObjectItem)value).Text = mapItem.Properties["akey?"].ToString();
                        }
                        cotGuidGenerate = null;
                    }
                }
            });
        }
        public void MapViewController_WheelMenuOpening(object sender, MenuPopupEventArgs e)
        {
            Log.i(TAG, "MapViewController_WheelMenuOpening() - Starting");

            // Log the type of 'sender'
            Log.i(TAG, "Sender Type: " + (sender?.GetType().ToString() ?? "null"));

            // Check if sender is WheelMenu
            if (sender is WheelMenu wheelMenu)
            {
                Log.i(TAG, "Sender is WheelMenu");


                // Attempt to get the clickedParentObject
                if (e.GetSingleClickedItem(out var clickedParentObject) != null)
                {
                    Log.i(TAG, "Clicked parent object is: " + clickedParentObject?.GetType().ToString());

                    // Check if clickedParentObject is MapItem
                    if (clickedParentObject is MapItem mapItem)
                    {
                        Log.i(TAG, "Clicked object is MapItem with Type : " + mapItem.GetType() + " _customMarkerCompositeMapItem : " + this._customMarkerCompositeMapItem.GetType());
                        Log.i(TAG, "Clicked object is MapItem with Name: " + mapItem.Name + " _customMarkerCompositeMapItem: " + this._customMarkerCompositeMapItem.Name);
                        Log.i(TAG, "Clicked object is MapItem with Text: " + mapItem.Properties + " _customMarkerCompositeMapItem: " + this._customMarkerCompositeMapItem.Properties);
                        Log.i(TAG, "Clicked object is MapItem with UID: " + mapItem.GetUid() + " _customMarkerCompositeMapItem: " + this._customMarkerCompositeMapItem.GetUid());

                        IDictionary<string, object> dictionaries = _customMarkerCompositeMapItem.Properties;

                        foreach (var dictionary in dictionaries)
                        {
                            Log.i(TAG, $"_customMarkerCompositeMapItem : Key: {dictionary.Key}, Value: {dictionary.Value}");
                        }

                        IDictionary<string, object> dictionaries2 = mapItem.Properties;
                        foreach (var dictionary in dictionaries2)
                        {
                            Log.i(TAG, $"mapItem : Key: {dictionary.Key}, Value: {dictionary.Value}");
                        }
                        //if (dictionary != null)
                        //{
                        //    // Iterate through all key-value pairs
                        //    foreach (var keyValuePair in dictionary)
                        //    {
                        //        Log.i(TAG, $"Key: {keyValuePair.Key}, Value: {keyValuePair.Value}");
                        //    }

                        //    // To access a specific key's value, for example

                        //}

                        // Check if the UIDs match
                        if (mapItem.GetUid().Equals(this._customMarkerCompositeMapItem.GetUid()))
                        {
                            Log.i(TAG, "MapItem UIDs match. Passed the first if statement.");

                            // Proceed with the rest of the logic
                            StandardActions.AddDelete(mapItem, wheelMenu, disabled: false);
                            WinTak.Graphics.MapMarker mapMarker = mapItem.GetMapMarker();

                            // Log if MapMarker is not null
                            if (mapMarker != null)
                            {
                                Log.i(TAG, "MapMarker is not null");
                                this._precisionMoveService.AddPrecisionMove(mapMarker, wheelMenu);
                            }
                            else
                            {
                                Log.i(TAG, "MapMarker is null");
                            }

                            // Remove the delete menu item
                            wheelMenu.Items.Remove(wheelMenu.Items.FirstOrDefault((WheelMenuItem x) => x.Id == "delete"));
                        }
                        else if (dictionaries2.TryGetValue("type", out var value))
                        {
                            if (value is string stringValue)
                            {
                                if (stringValue == "a-f-A")
                                {
                                    Log.i(TAG, "MapItem Type. Passed the else if statement.");

                                    // Proceed with the rest of the logic
                                    //StandardActions.AddDelete(mapItem, wheelMenu, disabled: false); // Create an empty space ? which shown on the figure previously
                                    WinTak.Graphics.MapMarker mapMarker = mapItem.GetMapMarker();

                                    WheelMenuItem _detailsWheelItem = new WheelMenuItem("brand_cthulhu", Application.Current.Resources[Icons.BreadcrumbActiveRadialMenuIconKey] as ImageSource, OnDetailsWheelItemClick)
                                    {
                                        Id = WinTak.Common.Properties.Resources.AddGeofence,
                                        ToolTip = "Test Sub Items Menu",
                                    };

                                    // Log if MapMarker is not null
                                    if (mapMarker != null)
                                    {
                                        Log.i(TAG, "MapMarker is not null");
                                        //this._precisionMoveService.AddPrecisionMove(mapMarker, wheelMenu); // Create an empty space ? which shown on the figure previously
                                    }
                                    else
                                    {
                                        Log.i(TAG, "MapMarker is null");
                                    }

                                    // Remove the delete menu item
                                    //wheelMenu.Items.Remove(wheelMenu.Items.FirstOrDefault((WheelMenuItem x) => x.Id == "delete"));
                                    wheelMenu.Items.Add(_detailsWheelItem);
                                }
                            }
                        }
                        else
                        {
                            Log.i(TAG, "MapItem UIDs do not match");
                        }
                    }
                    else
                    {
                        Log.i(TAG, "Clicked parent object is not MapItem");
                    }
                }
                else
                {
                    Log.i(TAG, "GetSingleClickedItem returned null or no clicked parent object");
                }
            }
            else
            {
                Log.i(TAG, "Sender is not WheelMenu");
            }

            Log.i(TAG, "MapViewController_WheelMenuOpening() - End");
        }

        private void OnDetailsWheelItemClick(object sender, EventArgs e)
        {
            MapEngine.Interop.Util.Log.i(TAG, "OnDetailsWheelItemClick() start");

            Log.i(TAG, "Sender Type: " + (sender?.GetType().ToString() ?? "null"));

            // Do something when the WheelMenuItem is clicked
            object tag = ((WheelMenuItemBase)(WheelMenuItem)sender).Tag;
            MapItem val = (MapItem)((tag is MapItem) ? tag : null);
            if (((WheelMenuItem)sender).Tag is MapItem mapItem && mapItem.Properties.ContainsKey("uid"))
            {
                Guid id = new Guid(mapItem.Properties["uid"].ToString());
                //ShowDockPane(id);
            }
        }

        private void OnDemandExecuted_ScanAnimatedQrBtn(object sender, EventArgs e)
        {
            try
            {
                // 1) Open scanner window (modal)
                var win = new ipswintakplugin.AnimatedQr.AnimatedQrScanWindow
                {
                    Owner = Application.Current?.MainWindow
                };

                win.ShowDialog();

                var result = win.Result;
                if (result == null)
                {
                    Toast.Show("Animated QR scan cancelled.");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(result.DecodeError))
                {
                    Prompt.Show("Animated QR decoded with issues:\n" + result.DecodeError);
                    Log.i(TAG, "AQR decode error: " + result.DecodeError);
                    // continue anyway if payload exists
                }

                if (string.IsNullOrWhiteSpace(result.DecodedUtf8))
                {
                    Prompt.Show("Animated QR scan produced no decoded payload.");
                    return;
                }

                // 2) Determine location (current GPS)
                var pos = _locationService.GetGpsPosition();
                var geoPoint = new TAKEngine.Core.GeoPoint(pos)
                {
                    Altitude = _elevationManager.GetElevation(pos),
                    AltitudeRef = global::TAKEngine.Core.AltitudeReference.HAE
                };
                if (double.IsNaN(geoPoint.Altitude)) geoPoint.Altitude = Altitude.UNKNOWN_VALUE;

                // 3) Build callsign
                var patientName = TryExtractPatientNameFromFhirBundle(result.DecodedUtf8);
                var callsign = string.IsNullOrWhiteSpace(patientName) ? "Patient" : $"Patient: {patientName}";
                callsign = _coTManager.CreateCallsign(callsign, CallsignCreationMethod.BasedOnTypeAndDate);

                // 4) CoT payload: gzip+base64 the decoded UTF8 (keep compatible with your existing inspection flow)
                var gzB64 = CompressUtf8ToBase64Gzip(result.DecodedUtf8);

                // 5) Create/Publish CoT marker (local add + network send)
                var uid = Guid.NewGuid().ToString();
                var type = "a-f-A"; // same as your “patient-ish” marker type
                var how = "h-g";

                // Put ipsData in detail; keep it simple and aligned with your other code
                var detail = $@"<detail>
                      <contact callsign=""{System.Security.SecurityElement.Escape(callsign)}"" />
                      <ipsData encoding=""gzipBase64"">{gzB64}</ipsData>
                      <_IPS_ title=""{System.Security.SecurityElement.Escape(callsign)}"" />
                      <precisionlocation altsrc=""DTED0"" />
                    </detail>";

                // Add to map (creates marker)
                _coTManager.AddItem(uid, type, geoPoint, callsign, detail);

                // If you also want to explicitly TX the CoT, uncomment and adapt based on your ICotMessageSender API:
                // var cot = new CotEvent { Uid = uid, Type = type, How = how };
                // cot.Point.Latitude = geoPoint.Latitude;
                // cot.Point.Longitude = geoPoint.Longitude;
                // cot.Point.Altitude = geoPoint.Altitude;
                // cot.Detail = detail; // (property name may differ)
                // _cotMessageSender.Send(cot); // adapt to actual method in your SDK

                Toast.Show("Published Patient CoT from Animated QR.");
            }
            catch (Exception ex)
            {
                Log.e(TAG, "Animated QR scan/publish failed: " + ex);
                Prompt.Show("Animated QR scan/publish failed:\n" + ex.Message);
            }
        }

        /* Layout Example - Recycler View
            * --------------------------------------------------------------------
            * Desc. :
            * */
        private void OnDemandExecuted_RecyclerViewBtn(object sender, EventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");

            Prompt.Show("Place a custom marker on the map.");
            _mapGroupManager.ItemAdded += MapCustomObjectAdded;
            MapViewControl.PushMapEvents(MapMouseEvents.MapMouseDown
                                        | MapMouseEvents.MapMouseMove
                                        | MapMouseEvents.MapMouseUp
                                        | MapMouseEvents.ItemDrag
                                        | MapMouseEvents.ItemDragCompleted
                                        | MapMouseEvents.ItemLongPress
                                        | MapMouseEvents.MapDrag
                                        | MapMouseEvents.MapLongPress
                                        | MapMouseEvents.MapDoubleClick
                                        | MapMouseEvents.ItemDoubleClick);
            MapViewControl.MapClick += PlaceCustomIPS_MapClick;
        }
        private void PlaceCustomIPS_MapClick(object sender, MapMouseEventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");

            /* Variables declaration */
            string cotUid;
            string cotType = "c-f-u";
            string cotName = "CHWM"; // IPSMarker
            string cotDetail;

            /* Implementation */
            cotUid = Guid.NewGuid().ToString();
            cotName = _coTManager.CreateCallsign(cotName, CallsignCreationMethod.BasedOnTypeAndDate);
            cotDetail = "<archive /> <_IPS_ title=\"" + cotName + "\" /><precisionlocation altsrc=\"DTED0\" />";
            TAKEngine.Core.GeoPoint geoPoint;
            geoPoint = new TAKEngine.Core.GeoPoint(e.WorldLocation)
            {
                Altitude = _elevationManager.GetElevation(e.WorldLocation),
                AltitudeRef = global::TAKEngine.Core.AltitudeReference.HAE
            };

            if (double.IsNaN(geoPoint.Altitude))
            {
                geoPoint.Altitude = Altitude.UNKNOWN_VALUE;
            }

            cotGuidGenerate = cotUid;
            _coTManager.AddItem(cotUid, cotType, geoPoint, cotName, cotDetail);

            MapItem value = null;
            value = CreateCustomMarkerRenderable(value, Guid.NewGuid(), "testCustom", geoPoint);
            Log.i(TAG, "MapItem : " + value.ToString() + "Geopoint : " + geoPoint.ToString());
            AddMapObjectItem(value, geoPoint);

            MapViewControl.PopMapEvents();
            Prompt.Clear();
        }

        private void MapCustomObjectAdded(object sender, MapItemEventArgs args)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");

            MapItem mapItem;
            mapItem = args?.MapItem;

            if (mapItem == null || cotGuidGenerate == null || cotGuidGenerate == mapItem.GetUid())
            {
                return;
            }
            base.DispatchAsync(delegate
            {
                if (_dockingManager.GetDockPane(ID) is IPSDockPane IPSDockPane)
                {
                    WinTak.Graphics.MapMarker mapMarker;
                    mapMarker = mapItem.GetMapMarker();
                    if (mapMarker != null)
                    {
                        // IPSDockPane.SetMarker(mapMarker);
                        mapItem.Properties.TryGetValue("IPSMapItem", out var value);
                        if (value != null)
                        {
                            ((MapObjectItem)value).Text = mapItem.Properties["akey?"].ToString();
                        }
                        cotGuidGenerate = null;

                    }

                    //MapItem value = null;
                    //value = CreateCustomMarkerRenderable();
                    //TAKEngine.Core.GeoPoint geoPoint = new TAKEngine.Core.GeoPoint(MapMouseEventArgs.WorldLocation)
                    //{
                    //    Altitude = _elevationManager.GetElevation(MapMouseEventArgs.WorldLocation),
                    //    AltitudeRef = global::TAKEngine.Core.AltitudeReference.HAE
                    //};

                    //AddMapObjectItem(value, geoPoint);
                }
            });
        }
        private void CustomHWOnDisposing(object sender, EventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");
            _customMarkerCompositeMapItem.Disposing -= CustomHWOnDisposing;
            _customMarkerCompositeMapItem = null;
        }

        private void AddMapObjectItem(MapItem shape, TAKEngine.Core.GeoPoint center)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");
            MapItem mapItem = shape;
            Log.i(TAG, "shape : " + shape.ToString());// + "Parent : " + shape.Parent.ToString());
            Log.i(TAG, "MapItem : " + mapItem.ToString() + "Geopoint : " + center.ToString());
            if (mapItem.Properties.TryGetValue("overlay-manager-entry", out var value))
            {
                ((MapObjectItem)value).Position = center;
                return;
            }

            LegacyMapObjectItem legacyMapObjectItem = new LegacyMapObjectItem(mapItem.GetCallsign(), null);
            legacyMapObjectItem.MapItem = mapItem;
            legacyMapObjectItem.Position = center;
            legacyMapObjectItem.Selectable = false;
            legacyMapObjectItem.Properties["exportable"] = false;
            _customMarker.Children.Add(legacyMapObjectItem);
            mapItem.Properties["overlay-manager-entry"] = legacyMapObjectItem;
        }
        private MapItem CreateCustomMarkerRenderable()
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");

            WinTak.Graphics.MapMarker mapMarker = CreateMarker();
            CompositeMapItem compositeMapItem = CreateMapObject(Guid.NewGuid().ToString(), "CustomHW_Test");
            _mapGroup.AddItem(compositeMapItem);

            return CreateMarker();
        }

        private MapItem CreateCustomMarkerRenderable(MapItem shape, Guid id, string title, TAKEngine.Core.GeoPoint center)
        {
            WinTak.Graphics.MapMarker mapMarker;
            if (shape == null)
            {
                mapMarker = CreateMarker();
                CompositeMapItem compositeMapItem = CreateMapObject(id.ToString(), title);
                compositeMapItem.AddItem("hwMarker", mapMarker);
                _mapGroup.AddItem(compositeMapItem);
            }
            else
            {
                mapMarker = (WinTak.Graphics.MapMarker)shape;
            }
            mapMarker.Position = center;
            return mapMarker;
        }

        private CompositeMapItem CreateMapObject(string id, string callsign = null)
        {
            CompositeMapItem compositeMapItem = new CompositeMapItem();
            compositeMapItem.Properties["uid"] = id;
            compositeMapItem.Properties["callsign"] = callsign ?? id;
            compositeMapItem.Properties["type"] = "h-w-c-m";
            compositeMapItem.Disposing += OnMapObjectDisposed;
            return compositeMapItem;
        }

        private void OnMapObjectDisposed(object sender, EventArgs e)
        {
            MapItem mapItem = (MapItem)sender;
            mapItem.Disposing -= OnMapObjectDisposed;
            Guid id = new Guid(mapItem.Properties["uid"].ToString());
        }
        private static WinTak.Graphics.MapMarker CreateMarker()
        {
            return new WinTak.Graphics.MapMarker
            {
                Visible = true,
                IconPath = new Uri("pack://application:,,,/Hello World Sample;component/assets/brand_cthulhu.png"),
                Scaling = DisplayManager.UIScale
            };
        }

        private static string CompressUtf8ToBase64Gzip(string utf8)
        {
            var inputBytes = Encoding.UTF8.GetBytes(utf8);
            using var ms = new MemoryStream();
            using (var gz = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
            {
                gz.Write(inputBytes, 0, inputBytes.Length);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        private static string TryExtractPatientNameFromFhirBundle(string decodedUtf8)
        {
            try
            {
                var jo = Newtonsoft.Json.Linq.JObject.Parse(decodedUtf8);
                // very lightweight heuristic for your Bundle:
                // entry[].resource.resourceType == "Patient" then resource.name[0].family + given[0]
                var entries = jo["entry"] as Newtonsoft.Json.Linq.JArray;
                if (entries == null) return null;

                foreach (var e in entries)
                {
                    var res = e["resource"] as Newtonsoft.Json.Linq.JObject;
                    if (res?["resourceType"]?.ToString() != "Patient") continue;

                    var name0 = res["name"]?.First;
                    var fam = name0?["family"]?.ToString();
                    var given0 = name0?["given"]?.First?.ToString();

                    var full = $"{given0} {fam}".Trim();
                    return string.IsNullOrWhiteSpace(full) ? null : full;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }


        // --------------------------------------------------------------------
        // Marker Manipulation
        // --------------------------------------------------------------------
        /* Marker Manipulation - Special Marker
         * --------------------------------------------------------------------
         * Desc. :
         * */
        private void OnDemandExecuted_SpecialMarkerButton(object sender, EventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");
            CotEvent cot = new CotEvent
            {
                Uid = Guid.NewGuid().ToString(),
                Type = "a-f-G-U-C-I",
                How = "h-g-i-g-o",

            };
            cot.Point.Latitude = 51.0;
            cot.Point.Longitude = 2.0;
            cot.Point.Altitude = 100.0;

            Log.i(TAG, "Testing CoTEvent() : " + cot.ToXml());
            Log.i(TAG, "Testing CoTEvent() : " + cot.ToString());


            Log.i(TAG, "TESTING SOMETHING 1 : " + _locationService.GetGpsPosition().ToString());
            Log.i(TAG, "TESTING SOMETHING 2 : " + _devicePreferences.Callsign);
            Log.i(TAG, "device interface was tested to ensure that we can get something ?");
        }

        /* Marker Manipulation - Add Streams
         * --------------------------------------------------------------------
         * Desc. :
         * */
        private void OnDemandExecuted_AddStreamBtn(object sender, EventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + " enable receiving CoT Message.");
            _cotMessageReceiver.MessageReceived += OnCotMessageReceived;

        }

        private const int MAX_LOG_LENGTH = 497;

        private void LogLongMessage(string tag, string message)
        {
            Log.i(tag, message.Length.ToString());
            if (message.Length > MAX_LOG_LENGTH)
            {
                int start = 0;
                while (start < message.Length)
                {
                    int chunkLength = Math.Min(MAX_LOG_LENGTH, message.Length - start);
                    // Using Log.Debug or similar depending on your logging framework
                    Log.i(tag, message.Substring(start, chunkLength));
                    Log.d(tag, message.Substring(start, chunkLength));
                    start += MAX_LOG_LENGTH;
                }
            }
            else
            {
                Log.i(tag, message);
                Log.d(tag, message);
            }
        }

        private void OnCotMessageReceived(object sender, CoTMessageArgument args)
        {
            LogLongMessage(TAG, args.Message.ToString());
            LogLongMessage(TAG, args.CotEvent.ToString());
            LogLongMessage(TAG, args.Type.ToString());
        }

        /* Marker Manipulation - Map Item (CoT) Inspect
         * --------------------------------------------------------------------
         * Desc. :
         * */



        // Now, in your button event handler (or another command), you can await the user's selection.
        //public string CotInspectionButtonText
        //{
        //    get { return _isCotInspectionActive ? "Hide COT Inspection" : "Map Item (IPS) Inspect"; }
        //}

        public static string DecompressBase64Gzip(string base64GzippedData)
        {
            // First, decode the Base64 string into bytes.
            byte[] gzipBytes = Convert.FromBase64String(base64GzippedData);

            // Then, create a MemoryStream over these bytes.
            using (var inputStream = new MemoryStream(gzipBytes))
            // Create a GZipStream for decompression.
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            // Read the decompressed bytes from the GZip stream.
            using (var reader = new StreamReader(gzipStream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private async void OnDemandExecuted_ItemInspectBtn(object sender, EventArgs e)
        {
            Log.i(TAG, "ItemInspectBtn pressed: awaiting map object selection.");

            if (!_isCotInspectionActive)
            {
                // Turn on CoT inspection mode.
                _isCotInspectionActive = true;
                //OnPropertyChanged(nameof(CotInspectionButtonText)); // Notify the UI to update the button text.


                // Show instructions.
                Prompt.Show("Tap on a map object to view its IPS data.");

                // Initialize the TaskCompletionSource to wait for selection.
                _selectionTcs = new TaskCompletionSource<MapItem>();

                // Subscribe to the map selection event.
                // (Assuming _mapViewController exposes WheelMenuOpening that fires when an object is selected.)
                _mapViewController.WheelMenuOpening += OnWheelMenuForSelection;

                // Await the user's selection.
                MapItem selectedItem = await _selectionTcs.Task;

                // Unsubscribe from the event once selection is done.
                _mapViewController.WheelMenuOpening -= OnWheelMenuForSelection;
                _mapViewController.DefaultWheelMenu.Hide();

                // Process the selected object, if any.
                if (selectedItem != null)
                {
                    // Hide the WheelMenu

                    if (selectedItem.Properties.TryGetValue("cot-event", out object cotDataObj))
                    {
                        string cotData = cotDataObj.ToString();
                        try
                        {
                            // Parse the entire COT XML.
                            XDocument xDoc = XDocument.Parse(cotData);
                            // Locate the ipsData element. Adjust the element name if needed.
                            var ipsDataElement = xDoc.Descendants("ipsData").FirstOrDefault();
                            if (ipsDataElement != null)
                            {
                                // ipsDataElement.Value is the Base64 gzipped string.
                                string base64Data = ipsDataElement.Value;
                                try
                                {
                                    string jsonData = DecompressBase64Gzip(base64Data);
                                    // Display the original JSON data.
                                    //Prompt.Show("Original JSON Data:\n" + jsonData);
                                    IPSDataWindow dataWindow = new IPSDataWindow(jsonData);
                                    dataWindow.ShowDialog();
                                    Log.i(TAG, "Original JSON Data: " + jsonData);
                                }
                                catch (Exception ex)
                                {
                                    Log.e(TAG, "Error decompressing ipsData: " + ex.ToString());
                                    Prompt.Show("Error decompressing ipsData: " + ex.Message);
                                }
                            }
                            else
                            {
                                Prompt.Show("The CoT data does not contain an ipsData element.");
                                Log.i(TAG, "No ipsData element found in the CoT data.");
                            }

                        }
                        catch (Exception ex)
                        {
                            Log.e(TAG, "Error parsing CoT data: " + ex.ToString());
                            Prompt.Show("Error parsing CoT data: " + ex.Message);
                        }
                    }
                    else
                    {
                        Prompt.Show("Selected object does not have associated CoT data.");
                        Log.i(TAG, "No 'cot-event' property found on selected object.");
                    }
                }

                // Inspection mode remains active and the CoT data stays visible until the user toggles off.
            }
            else
            {
                // If already active, toggle off: hide the CoT display and exit inspection mode.
                _isCotInspectionActive = false;
                //OnPropertyChanged(nameof(CotInspectionButtonText));

                // If waiting for a selection, force completion with null.
                _selectionTcs?.TrySetResult(null);

                // Clear the displayed prompt/message.
                Prompt.Clear();
            }
        }

        // --------------------------------------------------------------------
        // Elevation Examples
        // --------------------------------------------------------------------
        /* Elevation Examples - Query Surface Data
         * --------------------------------------------------------------------
         * Desc. :
         * */
        private void OnDemandExecuted_SurfaceAtCenterBtn(object sender, EventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");
        }
        // --------------------------------------------------------------------
        // Notification Examples
        // --------------------------------------------------------------------
        private void NotificationExamples_Configuration()
        {
            // Notification Examples - Get Current Notifications
            var getCurrentNotificationsCommand = new ExecutedCommand();
            getCurrentNotificationsCommand.Executed += OnDemandExecuted_GetCurrentNotificationsBtn;
            GetCurrentNotificationsBtn = getCurrentNotificationsCommand;

            // Notification Examples - Fake Content Provider
            var fakeContentProviderCommand = new ExecutedCommand();
            fakeContentProviderCommand.Executed += OnDemandExecuted_FakeContentProviderBtn;
            FakeContentProviderBtn = fakeContentProviderCommand;

            // Notification Examples - Notification Spammer
            var notificationSpammerCommand = new ExecutedCommand();
            notificationSpammerCommand.Executed += OnDemandExecuted_NotificationSpammerBtn;
            NotificationSpammerBtn = notificationSpammerCommand;

            // Notification Examples - Notification with Options
            var notificationWithOptionsCommand = new ExecutedCommand();
            notificationWithOptionsCommand.Executed += OnDemandExecuted_NotificationWithOptionsBtn;
            NotificationWithOptionsBtn = notificationWithOptionsCommand;

            // Notification Examples - Notification to WinTak Toast
            var notificationToWinTakToastCommand = new ExecutedCommand();
            notificationToWinTakToastCommand.Executed += OnDemandExecuted_NotificationWinTakToastBtn;
            NotificationToWinTakToastBtn = notificationToWinTakToastCommand;

            // Notification Examples - Notification to Windows
            var notificationToWindowsCommand = new ExecutedCommand();
            notificationToWindowsCommand.Executed += OnDemandExecuted_NotificationToWindows;
            NotificationToWindowsBtn = notificationToWindowsCommand;


        }
        /* Notification Examples - Get Current Notifications
         * --------------------------------------------------------------------
         * Desc. : Get notifications from WinTak
         * */
        private void OnDemandExecuted_GetCurrentNotificationsBtn(object sender, EventArgs e)
        {
            WinTak.Alerts.Notifications.AlertNotification alertNotification;
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");
            foreach (WinTak.Framework.Notifications.Notification notification in _notificationLog.Notifications)
            {
                Log.i(TAG, MethodBase.GetCurrentMethod() + " current notification : " + notification.ToString() + " - " + notification.Message);
            }

        }

        /* Notification Examples - Fake Content Provider
         * --------------------------------------------------------------------
         * Desc. : Display a simple notification in WinTak
         * */
        private void OnDemandExecuted_FakeContentProviderBtn(object sender, EventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");

            IPSNotification hwNotification = new IPSNotification
            {
                Uid = ULIDGenerator.GenerateULID(),
                Type = ULIDGenerator.GenerateULID(),
                Key = ULIDGenerator.GenerateULID(),
                StartTime = DateTime.UtcNow,
                StaleTime = DateTime.UtcNow.AddMinutes(1),
                Viewed = false,

            };
            hwNotification.Message = "Display a WinTAK notification at " + hwNotification.StartTime.ToString();
            _notificationLog.AddNotification(hwNotification);
            Log.i(TAG, MethodBase.GetCurrentMethod() + "Notification start : " + hwNotification.StartTime.ToString() + " / end : " + hwNotification.StaleTime.ToString());

        }

        /* Notification Examples - Notification Spammer
         * --------------------------------------------------------------------
         * Desc. : Loop which send more notification to WinTak
         * */
        private async void OnDemandExecuted_NotificationSpammerBtn(object sender, EventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");

            for (int i = 0; i < 10; i++)
            {
                IPSNotification hwNotification = new IPSNotification
                {
                    Uid = ULIDGenerator.GenerateULID(),
                    Type = ULIDGenerator.GenerateULID(),
                    Key = ULIDGenerator.GenerateULID(),
                    StartTime = DateTime.UtcNow,
                    StaleTime = DateTime.UtcNow.AddMinutes(1),
                    Viewed = false,

                };
                hwNotification.Message = "Test Spammer " + (i + 1).ToString() + " - " + hwNotification.StartTime.ToString();
                _notificationLog.AddNotification(hwNotification);

                await Task.Delay(1000);
            }

        }

        /* Notification Examples - Notification with Options
         * --------------------------------------------------------------------
         * Desc. : Notification with a click which focus on map 
         * 
         * */
        // TODO : Notification Examples - Notification with Options : how we can send the user to the localisation of the notification(like range&bearing)
        private void OnDemandExecuted_NotificationWithOptionsBtn(object sender, EventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");
            IPSNotification hwNotification = new IPSNotification
            {
                Uid = ULIDGenerator.GenerateULID(),
                Type = ULIDGenerator.GenerateULID(),
                Key = ULIDGenerator.GenerateULID(),
                StartTime = DateTime.UtcNow,
                StaleTime = DateTime.UtcNow.AddMinutes(1),
                Viewed = false,

            };
            hwNotification.Message = "Display a WinTAK notification at " + hwNotification.StartTime.ToString() + " with possibility to focus on the Notification";
            _notificationLog.AddNotification(hwNotification);
            Log.i(TAG, MethodBase.GetCurrentMethod() + "Notification start : " + hwNotification.StartTime.ToString() + " / end : " + hwNotification.StaleTime.ToString());

        }

        /* Notification Examples - Notification to WinTak Toast
         * --------------------------------------------------------------------
         * Desc. : Notification with a click which focus on map 
         * 
         * */
        private void OnDemandExecuted_NotificationWinTakToastBtn(object sender, EventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");
            string test = "Hello World display a short popup Box on WinTAK.";
            Toast.Show(test);
        }
        /* Notification Examples - Notification to Windows
         * --------------------------------------------------------------------
         * Desc. : Send a notification to Windows Toast (Sidebar notification)
         * */
        private void OnDemandExecuted_NotificationToWindows(object sender, EventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");

            // Example 1 :
            new ToastContentBuilder()
                .AddArgument("action", "viewConversation")
                .AddArgument("conversationId", 9813)
                .AddText("Hello World sent you a picture")
                .AddText("Check this out from WinTak Hello World Plugin")
                .Show();

            // Example 2 :
            Bitmap hwIco = Properties.Resources.ic_launcher_24x24;
            string hwIcoFilePath = SaveImageToFile(hwIco, "ic_launcher_24x24.png");
            Uri hwIcoUri = new Uri(hwIcoFilePath);

            Bitmap hwImg = Properties.Resources.hw_notification_icon;
            string hwImgFilePath = SaveImageToFile(hwImg, "hw_notification_icon.png");
            Uri hwImgUri = new Uri(hwImgFilePath);

            new ToastContentBuilder()
                .AddArgument("IPSNotification")
                .AddText("Hello World Plugin Notification with icon and Image")
                .AddAppLogoOverride(hwIcoUri, ToastGenericAppLogoCrop.Circle)
                .AddInlineImage(hwImgUri)
                .AddButton(new ToastButton().SetContent("Acknowledge")
                .AddArgument("IPSNotificationAck"))
                .Show(toast =>
                {
                    toast.Tag = "HelloWordNotification";
                });
        }


        // --------------------------------------------------------------------
        // Web
        // --------------------------------------------------------------------
        /* Web View
         * --------------------------------------------------------------------
         * Desc. :
         * */
        private void OnDemandExecuted_WebViewBtn(object sender, EventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");
            //string url = "https://ipsmern-dep.azurewebsites.net/";
            string url = "http://localhost:3000/";
            WebViewWindow webViewWindow = new WebViewWindow(url);
            webViewWindow.Show();
        }

        private async Task<string> GetHtmlContentFromApi(string apiUrl)
        {
            var client = new HttpClient();

            // Set a common User-Agent so that the request looks like it’s coming from a modern browser.
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Safari/537.36");

            // You can also set other headers if necessary, for example Accept:
            client.DefaultRequestHeaders.Accept.TryParseAdd("text/html");

            // Send the GET request.
            var uri = new Uri(apiUrl);
            HttpResponseMessage response = await client.GetAsync(uri);

            // Log the status code for debugging.
            Log.i(TAG, "HTTP GET status code: " + response.StatusCode.ToString());

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                // Optionally read the response content (if any) for additional details.
                string errorContent = await response.Content.ReadAsStringAsync();
                Log.e(TAG, $"HTTP GET failed. Status: {response.StatusCode}, Response: {errorContent}");
                throw new Exception($"HTTP GET failed with status {response.StatusCode}");
            }
        }

        /* Web Record View
         * --------------------------------------------------------------------
         * Desc. :
         * */
        //private async void OnDemandExecuted_WebRecordViewBtn(object sender, EventArgs e)
        //{
        //    Log.i(TAG, MethodBase.GetCurrentMethod() + "");
        //    // Define the API URL.
        //    string apiUrl = "http://localhost:5000/tak/browser/381238c4-1f92-43a4-8734-4eb05de12bf0";

        //    string htmlContent = string.Empty;
        //    try
        //    {
        //        htmlContent = await GetHtmlContentFromApi(apiUrl);
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.e(TAG, "Error performing GET request: " + ex.ToString());
        //        MessageBox.Show("Error fetching content: " + ex.Message);
        //        return;
        //    }

        //    // Now open a window to display the HTML. 
        //    // (Assuming your WebViewWindow accepts HTML content via NavigateToString)
        //    WebViewWindow webViewWindow = new WebViewWindow(htmlContent, isHtmlContent: true);
        //    webViewWindow.Show();
        //}

        private async void OnDemandExecuted_WebRecordViewBtn(object sender, EventArgs e)
        {
            Log.i(TAG, "WebRecordViewBtn pressed: awaiting map object selection.");

            if (!_isCotInspectionActive)
            {
                // Turn on IPS inspection mode.
                _isCotInspectionActive = true;
                Prompt.Show("Tap on a map object to view IPS data from IPS MERN.");

                // Initialize the TaskCompletionSource to wait for selection.
                _selectionTcs = new TaskCompletionSource<MapItem>();

                // Subscribe to the map selection event.
                _mapViewController.WheelMenuOpening += OnWheelMenuForSelection;

                // Await the user's selection.
                MapItem selectedItem = await _selectionTcs.Task;

                // Unsubscribe once selection is done.
                _mapViewController.WheelMenuOpening -= OnWheelMenuForSelection;
                _mapViewController.DefaultWheelMenu.Hide();

                // Process the selected object.
                if (selectedItem != null)
                {
                    if (selectedItem.Properties.TryGetValue("cot-event", out object cotDataObj))
                    {
                        string cotData = cotDataObj.ToString();
                        try
                        {
                            // Parse the CoT XML.
                            XDocument xDoc = XDocument.Parse(cotData);
                            // Locate the ipsData element.
                            var ipsDataElement = xDoc.Descendants("ipsData").FirstOrDefault();
                            if (ipsDataElement != null)
                            {
                                // The ipsData element's value is the Base64 gzipped JSON.
                                string base64Data = ipsDataElement.Value;
                                try
                                {
                                    string jsonData = DecompressBase64Gzip(base64Data);
                                    Log.i(TAG, "Original JSON Data: " + jsonData);

                                    // Parse the JSON to extract the packageID.
                                    var jo = Newtonsoft.Json.Linq.JObject.Parse(jsonData);
                                    string packageId = jo["packageUUID"]?.ToString();
                                    if (string.IsNullOrWhiteSpace(packageId))
                                    {
                                        Prompt.Show("Could not find packageID in IPS data.");
                                        return;
                                    }

                                    // Build the URL for the GET API call.
                                    string browserUrl = $"http://localhost:5000/tak/browser/{packageId}";
                                    Log.i(TAG, "Navigating to URL: " + browserUrl);

                                    // Call the API and get the HTML content.
                                    string htmlContent = await GetHtmlContentFromApi(browserUrl);

                                    // Display the HTML content in the WebView window.
                                    WebViewWindow webViewWindow = new WebViewWindow(htmlContent, isHtmlContent: true);
                                    webViewWindow.Show();
                                }
                                catch (Exception ex)
                                {
                                    Log.e(TAG, "Error decompressing or parsing IPS JSON: " + ex.ToString());
                                    Prompt.Show("Error processing IPS data: " + ex.Message);
                                }
                            }
                            else
                            {
                                Prompt.Show("The CoT data does not contain an ipsData element.");
                                Log.i(TAG, "No ipsData element found in the CoT data.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.e(TAG, "Error parsing CoT data: " + ex.ToString());
                            Prompt.Show("Error parsing CoT data: " + ex.Message);
                        }
                    }
                    else
                    {
                        Prompt.Show("Selected object does not have associated CoT data.");
                        Log.i(TAG, "No 'cot-event' property found on selected object.");
                    }
                }

                // End inspection mode.
                _isCotInspectionActive = false;
                Prompt.Clear();
            }
            else
            {
                // If inspection mode is already active, toggle it off.
                _isCotInspectionActive = false;
                _selectionTcs?.TrySetResult(null);
                Prompt.Clear();
                _mapViewController.DefaultWheelMenu.Hide();
            }

            // Clean up map events.
            MapViewControl.PopMapEvents();
        }



        // --------------------------------------------------------------------
        // Plugin Template Duplicate
        // --------------------------------------------------------------------
        /* Plugin Template Duplicate - Counter
         * --------------------------------------------------------------------
         * Desc. :
         * */
        // This method is linked to the TextBlock where the counter value is displayed.
        public int Counter
        {
            get { return _counter; }
            set { SetProperty(ref _counter, value); }
        }
        // This method is linked to the Button when an OnClick() is done.
        private void OnDemandExecuted_IncreaseCounterBtn(object sender, EventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");
            Counter++;
        }

        /* Plugin Template Duplicate - (de)activate
         * --------------------------------------------------------------------
         * Desc. : This is an example of how to interact with the MapComponent
         *         on some part.
         * */
        public bool MapFunctionIsActivate
        {
            get { return _mapFunctionIsActivate; }
            set
            {
                SetProperty<bool>(ref _mapFunctionIsActivate, value, "MapFunctionIsActive");
                if (value)
                {
                    MapViewControl.MapMouseMove += MapViewControl_MapMouseMove;

                }
                else
                {
                    MapViewControl.MapMouseMove -= MapViewControl_MapMouseMove;
                }
            }
        }
        public double MapFunctionLon
        {
            get { return _mapFunctionLon; }
            set { SetProperty(ref _mapFunctionLon, value); }
        }
        public double MapFunctionLat
        {
            get { return _mapFunctionLat; }
            set { SetProperty(ref _mapFunctionLat, value); }
        }

        private void MapViewControl_MapMouseMove(object sender, MapMouseEventArgs e)
        {
            MapFunctionLat = e.WorldLocation.Latitude;
            MapFunctionLon = e.WorldLocation.Longitude;
        }

        /* Plugin Template Duplicate - White House
         * --------------------------------------------------------------------
         * Desc. : Client code can programmatically request WinTAK to focus 
         *         on a GeoPoint(s) (pan and zoom to). This and other actions 
         *         can be achieved with the WinTak.Framework.Messaging.IMessageHub 
         *         pub/sub interface.    
         * */
        private void OnDemandExecuted_WhiteHouseCoTBtn(object sender, EventArgs e)
        {
            Log.i(TAG, MethodBase.GetCurrentMethod() + "");

            var message = new FocusMapMessage(new TAKEngine.Core.GeoPoint(51.7, -2.99)) { Behavior = WinTak.Common.Events.MapFocusBehavior.PanOnly };
            _messageHub.Publish(message);
        }
    }
}
