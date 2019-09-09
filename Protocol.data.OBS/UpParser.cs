using Hydrology.Entity;
using Protocol.Data.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocol.Data.OBS
{
    public class UpParser : IUp
    {
        public static Dictionary<String, CEntityPackage> cEntityPackage = new Dictionary<string, CEntityPackage>();

        public bool Parse(string msg, out CReportStruct report)
        {
            report = new CReportStruct();
            //List<CReportData> dataList = new List<CReportData>();
            List<CReportObs> dataList = new List<CReportObs>();
            try
            {
                string data = string.Empty;
                //去除起始符'$'
                if (!ProtocolHelpers.DeleteSpecialCharLong(msg, out data))
                {
                    return false;
                }
                //站号（4位）
                string StationId = data.Substring(0, 10).Trim();
                //长度：1G
                string length = data.Substring(10, 4);

                DateTime recvTime;
                recvTime = new DateTime(
                               year: Int32.Parse("20" + data.Substring(14, 2)),
                               month: Int32.Parse(data.Substring(16, 2)),
                               day: Int32.Parse(data.Substring(18, 2)),
                               hour: Int32.Parse(data.Substring(20, 2)),
                               minute: Int32.Parse(data.Substring(22, 2)),
                               second: 0
                           );
                //上下行标志
                string type = data.Substring(24, 2);

                //报类（2位）：22-定时报
                string reportTypeString = data.Substring(26, 2);

                //站类
                string stationTypeString = data.Substring(28, 2);

                //水位
                string waterStr = data.Substring(30, 6);
                Decimal? water;
                if (waterStr.Contains("?"))
                {
                    water = null;
                }
                else
                {
                    water = Decimal.Parse(waterStr) / 100;
                }

                //雨量
                string rainStr = data.Substring(36, 4);
                Decimal? rain;
                if (rainStr.Contains("?"))
                {
                    rain = null;
                }
                else
                {
                    rain = Decimal.Parse(rainStr) / 100;
                }
                //电压
                string voltageStr = data.Substring(40, 4);
                Decimal? voltage;
                if (voltageStr.Contains("?"))
                {
                    voltage = null;
                }
                else
                {
                    voltage = Decimal.Parse(voltageStr) / 100;
                }

                //报类
                EMessageType reportType;
                reportType = ProtocolMaps.MessageTypeMap.FindKey(reportTypeString);
                //站类
                EStationType stationType;
                stationType = ProtocolMaps.StationTypeMap.FindKey(stationTypeString);

                string allElmtData = data.Substring(44);

                //1.处理obs信息
                //CReportData speedData = new CReportData();
                CReportObs obsData = new CReportObs();
                //1.1 obs信息
                int flagIndex = allElmtData.IndexOf("@@ OBS3A");

                int keyLength = int.Parse(allElmtData.Substring(8, 4));
                string elmtData = allElmtData.Substring(flagIndex, keyLength);
                //判定要素1的开始符号和结束符号
                if (elmtData.StartsWith("@@ OBS3A") && elmtData.EndsWith("**"))
                {
                    elmtData = elmtData.Substring(12, keyLength - 14).Trim();
                    //判定时差法数据的开始符号和接受符号

                    try
                    {
                        elmtData = new System.Text.RegularExpressions.Regex("[\\s]+").Replace(elmtData, " ");
                        string[] elmtDataList = elmtData.Split(' ');
                        if (elmtDataList.Length == 10)
                        {
                            decimal? deepth = decimal.Parse(elmtDataList[2]);
                            decimal? ntu = decimal.Parse(elmtDataList[3]);
                            decimal? mud = decimal.Parse(elmtDataList[4]);
                            decimal? tmp = decimal.Parse(elmtDataList[5]);
                            decimal? cndcty = decimal.Parse(elmtDataList[6]);
                            decimal? salinity = decimal.Parse(elmtDataList[7]);
                            decimal? batt = decimal.Parse(elmtDataList[8]);
                            obsData.Time = recvTime;
                            obsData.Depth = deepth;
                            obsData.Ntu = ntu;
                            obsData.Mud = mud;
                            obsData.Tmp = tmp;
                            obsData.Cndcty = cndcty;
                            obsData.Salinity = salinity;
                            obsData.Batt = batt;
                            obsData.Voltge = voltage;
                            dataList.Add(obsData);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        return false;
                    }

                    
                    
                    report.Stationid = StationId;
                    report.Type = type;
                    report.ReportType = reportType;
                    report.StationType = stationType;
                    report.RecvTime = recvTime;
                    report.obsDatas = dataList;
                }
                else
                {
                    return false;
                    //TODO 要素1开始符号和结束符合不匹配
                }

            }
            catch (Exception eee)
            {
                return false;
            }
            return true;
        }


        public bool Parse_1(string msg, out CReportStruct report)
        {
            throw new NotImplementedException();
        }

        public bool Parse_2(string msg, out CReportStruct report)
        {
            throw new NotImplementedException();
        }

        public bool Parse_beidou(string sid, EMessageType type, string msg, out CReportStruct upReport)
        {
            throw new NotImplementedException();
        }
    }
}
