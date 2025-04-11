using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinTak.Common.Geofence;
using ipswintakplugin.GeoFences;

namespace ipswintakplugin.Services
{
    internal interface IIPSServices
    {
        GeofencesMonitoring geofencesMonitoring { get; set; }
        void Dispose();
    }
}
