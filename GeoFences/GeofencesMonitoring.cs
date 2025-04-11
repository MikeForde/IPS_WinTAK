using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinTak.Common.Geofence;

namespace ipswintakplugin.GeoFences
{
    internal class GeofencesMonitoring
    {
        public static string overlayName = "Geo Fences";
        private GeofenceData geofenceData;
        private DateTime startDateTimeMonitoring;
        private DateTime endDateTimeMonitoring;

        //public GeofenceDataMonitoring(GeofenceData geofenceData, DateTime startDateTimeMonitoring, DateTime endDateTimeMonitoring)
        //{
        //    this.geofenceData = geofenceData;
        //    this.startDateTimeMonitoring = startDateTimeMonitoring;
        //    this.endDateTimeMonitoring = endDateTimeMonitoring;
        //}
    }
}
