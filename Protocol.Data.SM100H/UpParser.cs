/************************************************************************************
* Copyright (c) 2019 All Rights Reserved.
*命名空间：Protocol.Data.SM100H
*文件名： UpParser
*创建人： XXX
*创建时间：2019-7-17 19:38:20
*描述
*=====================================================================
*修改标记
*修改时间：2019-7-17 19:38:20
*修改人：XXX
*描述：
************************************************************************************/
using Hydrology.Entity;
using Protocol.Data.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocol.Data.SM100H
{
    public class UpParser : IUp
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

                //1.处理SM100H数据
                CReportData speedData = new CReportData();
                int flagIndex = allElmtData.IndexOf("@@SM100H");
                if (flagIndex >= 0)
                {
                    int keyLength = int.Parse(allElmtData.Substring(8, 4));
                    string elmtData = allElmtData.Substring(flagIndex, keyLength);
                    //判定要素1的开始符号和结束符号
                    if (elmtData.StartsWith("@@SM100H") && elmtData.EndsWith("**"))
                    {
                        elmtData = elmtData.Substring(12, keyLength - 14).Trim();
                        //判定时差法数据的开始符号和接受符号
                        if (elmtData.Length  == 78)
                        {

                            try
                            {
                                string waterSpeedStr = elmtData.Substring(38,8);
                                string waterFlowStr = elmtData.Substring(48, 8);
                                string waterFlow2Str = elmtData.Substring(58, 8);

                                //字符串转16进制32位无符号整数
                                UInt32 waterSpeedInt = Convert.ToUInt32(waterSpeedStr, 16);
                                UInt32 waterFlowInt = Convert.ToUInt32(waterFlowStr, 16);
                                UInt32 waterFlow2Int = Convert.ToUInt32(waterFlow2Str, 16);

                                //IEEE754 字节转换float
                                float waterSpeed = BitConverter.ToSingle(BitConverter.GetBytes(waterSpeedInt), 0);
                                float waterFlow = BitConverter.ToSingle(BitConverter.GetBytes(waterFlowInt), 0);
                                float waterFlow2 = BitConverter.ToSingle(BitConverter.GetBytes(waterFlow2Int), 0);

                                //UInt32 x = Convert.ToUInt32(waterFlowStr, 16);//字符串转16进制32位无符号整数
                                //float fy = BitConverter.ToSingle(BitConverter.GetBytes(x), 0);//IEEE754 字节转换float
                                //UInt32 x2 = Convert.ToUInt32(waterFlow2Str, 16);//字符串转16进制32位无符号整数
                                //float fy2 = BitConverter.ToSingle(BitConverter.GetBytes(x2), 0);//IEEE754 字节转换float
                                speedData.v1 = (decimal?)waterSpeed;
                                speedData.Q = (decimal?)waterFlow;
                                speedData.Q2 = (decimal?)waterFlow2;
                                speedData.Voltge = voltage;
                                dataList.Add(speedData);


                            }
                            catch(Exception e)
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