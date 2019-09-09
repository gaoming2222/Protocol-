using Hydrology.Entity;
using Protocol.Data.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocol.Data.PWD50
{
    class UpParser : IUp
    {
        public static Dictionary<String, CEntityPackage> cEntityPackage = new Dictionary<string, CEntityPackage>();

        public bool Parse(string msg, out CReportStruct report)
        {
            report = new CReportStruct();
            List<CReportData> dataList = new List<CReportData>();
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

                //1.处理风速风向数据和散射仪数据
                CReportData speedData = new CReportData();
                //1.1 风速风向信息
                int flagIndex = allElmtData.IndexOf("@@  EN2B");
                if (flagIndex >= 0)
                {
                    int keyLength = int.Parse(allElmtData.Substring(8, 4));
                    string elmtData = allElmtData.Substring(flagIndex, keyLength);
                    //判定要素1的开始符号和结束符号
                    if (elmtData.StartsWith("@@  EN2B") && elmtData.EndsWith("**"))
                    {
                        elmtData = elmtData.Substring(12, keyLength - 14).Trim();
                        //判定时差法数据的开始符号和接受符号
                        if (elmtData.Length == (keyLength - 14))
                        {

                            try
                            {
                                string strtflag = elmtData.Substring(0, 1);//开始标志
                                string stationid = elmtData.Substring(1, 5);//站点ID
                                string msgTime = elmtData.Substring(6, 12);//时间
                                string shfx = elmtData.Substring(18, 4);//瞬时风向
                                string shfs = elmtData.Substring(22, 4);//顺时风速
                                string yxszdshfx = elmtData.Substring(26, 4);//一小时最大瞬时风向
                                string yxszdshfs = elmtData.Substring(30, 4);//一小时最大瞬时风速
                                string maxTime = elmtData.Substring(34, 4); //一小时最大瞬时风速出现时间
                                string avg2fx = elmtData.Substring(38, 4); //2分钟平均风向
                                string avg2fs = elmtData.Substring(42, 4);//2分钟平均风速
                                string avg10fx = elmtData.Substring(46, 4);//10分钟平均风向
                                string avg10fs = elmtData.Substring(50, 4);//10分钟平均风速
                                string max10fx = elmtData.Substring(54, 4);//10分钟平均最大风向
                                string max10fs = elmtData.Substring(58, 4);//10分钟平均最大风速
                                string max10tm = elmtData.Substring(62, 4);//10分钟最大风速出现时间

                            }
                            catch (Exception e)
                            {
                                return false;
                            }

                        }
                        else
                        {
                            return false;
                            //11开头  99结束
                        }
                        report.Stationid = StationId;
                        report.Type = type;
                        report.ReportType = reportType;
                        report.StationType = stationType;
                        report.RecvTime = recvTime;
                        report.Datas = dataList;
                    }
                    else
                    {
                        return false;
                        //TODO 要素1开始符号和结束符合不匹配
                    }
                }
                //1.2 散射仪数据
                int flagIndex2 = allElmtData.IndexOf("@@   PWD");
                if (flagIndex >= 0)
                {
                    int keyLength = int.Parse(allElmtData.Substring(8, 4));
                    string elmtData = allElmtData.Substring(flagIndex, keyLength);
                    if (elmtData.StartsWith("@@  EN2B") && elmtData.EndsWith("**"))
                    {

                    }
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
