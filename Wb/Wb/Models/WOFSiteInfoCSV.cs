﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Profile;
using Wb.hiscentral;
using Wb.wof_1_1;
using WebGrease;

namespace Wb.Models
{
    // set of classes to write out CSV files

   

    namespace formatters
    {
        public class Wof11_SiteInfoCsvFormatter : BufferedMediaTypeFormatter
        {
            public Wof11_SiteInfoCsvFormatter()
            {
                // Add the supported media type.
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/csv"));
            }


            public override bool CanWriteType(System.Type type)
            {
                if (type == typeof(SiteInfoResponseTypeSite))
                {
                    return true;
                }
                else
                {
                    Type enumerableType = typeof(IEnumerable<SiteInfoResponseTypeSite>);
                    return enumerableType.IsAssignableFrom(type);
                }
            }
            public override bool CanReadType(Type type)
            {
                return false;
            }

            public override void WriteToStream(Type type, object value, Stream writeStream, HttpContent content)
            {

                using (var writer = new StreamWriter(writeStream))
                {
                    var products = value as IEnumerable<SiteInfoResponseTypeSite>;
                    if (products != null)
                    {
                        foreach (var product in products)
                        {
                            WriteItem(product, writer);
                        }
                    }
                    else
                    {
                        var singleProduct = value as SiteInfoResponseTypeSite;
                        if (singleProduct == null)
                        {
                            throw new InvalidOperationException("Cannot serialize type");
                        }
                        WriteItem(singleProduct, writer);
                    }
                }
            }
            // Helper methods for serializing Products to CSV format. 
            private void WriteItem(SiteInfoResponseTypeSite siteInfo, StreamWriter writer)
            {
                var siteCode = siteInfo.siteInfo.siteCode.First().Value;
                var sitenetwork = siteInfo.siteInfo.siteCode.First().network;
                var siteName = siteInfo.siteInfo.siteName;
                var siteType = siteInfo.siteInfo.siteType;
                double lat;
                double lon ;

                try
                {
                    var loc = siteInfo.siteInfo.geoLocation.geogLocation;
                    if (loc.GetType().IsAssignableFrom(typeof (LatLonPointType)))
                    {
                        var pt = loc as LatLonPointType;
                        lat = pt.latitude;
                        lon = pt.longitude;
                    }
                    else
                    {
                        lat = -9999;
                        lon = -9999;
                    }
                }
                catch
                {
                    // bad site
                    writer.WriteLine(
                        FormattedSeriesRow(network : sitenetwork, siteCode : siteCode, siteName:siteName)
                        );
                    return;
                }

                if (siteInfo != null ){
                    if (siteInfo.seriesCatalog != null)
                    {
                        foreach (var catalog in siteInfo.seriesCatalog)
                        {
                            if (catalog.series != null)
                            {
                                var servUrl = catalog.serviceWsdl;
                                foreach (var s in catalog.series)
                                {
                                    var startTime = s.variableTimeInterval.beginDateTimeUTC;
                                    var endtime = s.variableTimeInterval.endDateTimeUTC;
                                    var variableCode = s.variable.variableCode.First().Value;
                                    var variableVocab = s.variable.variableCode.First().vocabulary;

                                    writer.WriteLine(
                                        FormattedSeriesRow(network: sitenetwork, siteCode: siteCode, siteName: siteName,
                                        lat:lat.ToString(), lon:lon.ToString(),
                                        variableCode:variableCode, variableVocab:variableVocab,
                                        startTime: startTime.ToString("yyyy-MM-dd"), endtime: endtime.ToString("yyyy-MM-dd")
                                        )
                                        ); 
                                    
                                    //writer.WriteLine("{0},{1},{2},{3},{4},{5}", Escape(siteCode), Escape(siteName),
                                    //    Escape(variableCode), Escape(variableVocab), Escape(startTime), Escape(endtime));
                                }
                            }
                            else
                            {
                                writer.WriteLine("# no series for site {0} {1}", Escape(siteCode), Escape(siteName));
                            }
                        }
                    }
                    else
                    {
                        // only a site
                        writer.WriteLine(
                            FormattedSeriesRow(network: sitenetwork, siteCode: siteCode, siteName: siteName,
                            lat:lat.ToString(), lon:lon.ToString())
                            );
                    }
                }
            }

            private string FormattedSeriesRow(string siteName = "",
                string siteCode = "",
                String variableCode = "", string network = "",
                string startTime = "", string endtime = "", string variableName = "",
                string variableVocab="", string lat="", string lon="" )
            {

                return String.Format("{0}:{1},{2},{3},{4},{5},{6},{7},{8}", Escape(network), Escape(siteCode), Escape(siteName), lat, lon,
                    variableVocab, Escape(variableCode), Escape(startTime), Escape(endtime));
            }
            static char[] _specialChars = new char[] { ',', '\n', '\r', '"' };

            private string Escape(object o)
            {
                if (o == null)
                {
                    return "";
                }
                string field = o.ToString();
                if (field.IndexOfAny(_specialChars) != -1)
                {
                    // Delimit the entire field with quotes and replace embedded quotes with "".
                    return String.Format("\"{0}\"", field.Replace("\"", "\"\""));
                }
                else return field;
            }



        }  //namespace foramtter
    }
}