/************************************************************************************
* Copyright (c) 2019 All Rights Reserved.
*命名空间：Protocol.Data.TDXY
*文件名： UpParser
*创建人： XXX
*创建时间：2019-6-25 9:01:29
*描述
*=====================================================================
*修改标记
*修改时间：2019-6-25 9:01:29
*修改人：XXX
*描述：
************************************************************************************/
using System;
using System.Collections.Generic;
using Hydrology.Entity;
using Protocol.Data.Interface;
using System.Diagnostics;

namespace Protocol.Data.TDXY
{
    public class UpParser : IUp
    {
        //用于保存包序号，并写入缓存
        public static Dictionary<String, CEntityPackage> cEntityPackage = new Dictionary<string, CEntityPackage>();

        /// <summary>
        /// 非卫星报文数据解析过程
        /// </summary>
        /// <param name="msg">原始报文数据</param>
        /// <param name="report">报文最终解析出的结果数据结构</param>
        /// <returns>是否解析成功</returns>
        public bool Parse(String msg, out CReportStruct report)
        {
            //$      1900020919060308101G21??????????1066@@  QTFM0164        11     0.163     1.171     2.546     0.000     0.334     0.000     0.000    -999.0      -0.2       0.0       0.0      14.7       1.5        99**
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
                //类别（长度）：1G
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

                //水位
                string waterStr = data.Substring(28, 6);
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
                string rainStr = data.Substring(34, 4);
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
                string voltageStr = data.Substring(38, 4);
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
                stationType = EStationType.EO;
                string allElmtData = data.Substring(42);

                //1.处理时差法数据
                CReportData speedData = new CReportData();
                int flagIndex = allElmtData.IndexOf("@@  QTFM");
                if (flagIndex >= 0)
                {
                    int keyLength = int.Parse(allElmtData.Substring(8, 4));
                    string elmtData = allElmtData.Substring(flagIndex, keyLength);
                    //判定要素1的开始符号和结束符号
                    if (elmtData.StartsWith("@@  QTFM") && elmtData.EndsWith("**"))
                    {
                        elmtData = elmtData.Substring(12, keyLength - 14).Trim();
                        //判定时差法数据的开始符号和接受符号
                        if (elmtData.StartsWith("11") && elmtData.EndsWith("99"))
                        {
                            try
                            {
                                elmtData = new System.Text.RegularExpressions.Regex("[\\s]+").Replace(elmtData, " ");
                                string[] elmtDataList = elmtData.Split(' ');
                                speedData.Voltge = voltage;
                                speedData.Vm = Decimal.Parse(elmtDataList[1]);
                                speedData.W1 = Decimal.Parse(elmtDataList[2]);
                                speedData.Q = Decimal.Parse(elmtDataList[3]);
                                speedData.v1 = Decimal.Parse(elmtDataList[4]);
                                speedData.v2 = Decimal.Parse(elmtDataList[5]);
                                speedData.v3 = Decimal.Parse(elmtDataList[6]);
                                speedData.v4 = Decimal.Parse(elmtDataList[7]);
                                speedData.beta1 = Decimal.Parse(elmtDataList[8]);
                                speedData.beta2 = Decimal.Parse(elmtDataList[9]);
                                speedData.beta3 = Decimal.Parse(elmtDataList[10]);
                                speedData.beta4 = Decimal.Parse(elmtDataList[11]);
                                speedData.W2 = Decimal.Parse(elmtDataList[12]);
                                speedData.errorCode = elmtDataList[13];
                                dataList.Add(speedData);
                            }
                            catch (Exception ee)
                            {
                                //解析失败
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

        /// <summary>
        /// 卫星报文数据解析过程，是在非卫星报文数据的基础上增加了BCD编码。
        /// </summary>
        /// <param name="msg">原始报文数据</param>
        /// <param name="upReport">报文最终解析出的结果数据结构</param>
        /// <returns>是否解析成功</returns>
        public bool Parse_beidou(string sid, EMessageType msgType, string msg, out CReportStruct upReport)
        {
            // 这里stationId和type暂时不能获取，写成固定值。
            //string stationId = "9999";
            string type = "1G"; ;
            string appendMsg = "$" + sid + type + ProtocolHelpers.dealBCD(msg) + CSpecialChars.ENTER_CHAR;
            return Parse(appendMsg, out upReport);
        }

        /// <summary>
        /// 针对后面的连续数据项，逐个数据项进行填充，最后将数据结果作为list返回。
        /// </summary>
        /// <param name="list">原数据字符串</param>
        /// <param name="recvtime">接收数据时间，作为数据项的起始时间逐个往前推算。</param>
        /// <param name="voltage">电压值，每个数据项都要填充上电压信息</param>
        /// <param name="stationType">站点类型，作为读取数据部分的依据：需要读取雨量还是水位信息。</param>
        /// <returns>列表形式的数据集</returns>
        private List<CReportData> GetData(string list, DateTime recvtime, decimal voltage, EStationType stationType)
        {
            var result = new List<CReportData>();
            string tmp;
            for (int i = 0; i < 12; i++)
            {
                CReportData data = new CReportData();
                tmp = list.Substring(i * 10, 10);
                if (FillData(tmp, recvtime, voltage, stationType, out data))
                {
                    result.Add(data);
                }
                recvtime = recvtime.AddMinutes(-5);
            }
            // 将结果中的数据顺序逆置
            result.Reverse();
            return result;
        }

        /// <summary>
        /// 半小时6条数据
        /// </summary>
        /// <param name="list"></param>
        /// <param name="recvtime"></param>
        /// <param name="voltage"></param>
        /// <param name="stationType"></param>
        /// <returns></returns>
        private List<CReportData> GetData_1(string list, DateTime recvtime, decimal voltage, EStationType stationType)
        {
            var result = new List<CReportData>();
            string tmp;

            for (int i = 0; i < 6; i++)
            {
                CReportData data = new CReportData();
                if (list.Length >= (i * 10 + 10))
                {
                    tmp = list.Substring(i * 10, 10);
                    if (FillData(tmp, recvtime, voltage, stationType, out data))
                    {
                        result.Add(data);
                    }
                    recvtime = recvtime.AddMinutes(-5);
                }
            }
            // 将结果中的数据顺序逆置
            result.Reverse();
            return result;
        }

        private List<CReportData> GetData_2(string list, DateTime recvtime, decimal voltage, EStationType stationType)
        {
            var result = new List<CReportData>();
            string tmp;

            for (int i = 0; i < 1; i++)
            {
                CReportData data = new CReportData();
                if (list.Length >= (i * 10 + 10))
                {
                    tmp = list.Substring(i * 10, 10);
                    if (FillData(tmp, recvtime, voltage, stationType, out data))
                    {
                        result.Add(data);
                    }
                    recvtime = recvtime.AddMinutes(-5);
                }
            }
            // 将结果中的数据顺序逆置
            result.Reverse();
            return result;
        }

        private List<CReportData> GetAddData(IList<string> dataSegs, DateTime recvTime, EStationType stationType)
        {
            var result = new List<CReportData>();
            foreach (var item in dataSegs)
            {
                CReportData data = new CReportData();
                //  解析时间
                data.Time = recvTime;
                //  根据站点类型解析数据
                switch (stationType)
                {
                    case EStationType.ERainFall:
                        {
                            //  雨量
                            //  解析雨量                         单位mm，未乘以精度
                            try
                            {
                                Decimal rain = Decimal.Parse(item.Substring(6, 4));
                                data.Rain = rain;
                            }
                            catch (Exception e)
                            {
                                data.Rain = -1;
                            }
                        }
                        break;
                    case EStationType.EHydrology:
                        {
                            //  水文
                            //  解析雨量                         单位mm，未乘以精度
                            try
                            {
                                Decimal rain = Decimal.Parse(item.Substring(6, 4));
                                data.Rain = rain;
                            }
                            catch (Exception e)
                            {
                                data.Rain = -1;
                            }
                            //  解析水位  4(整数位) + 2(小数位)  单位m
                            try
                            {
                                Decimal water = Decimal.Parse(item.Substring(0, 6)) * (Decimal)0.01;
                                data.Water = water;
                            }
                            catch (Exception e)
                            {
                                data.Water = -200;
                            }
                        }
                        break;
                    case EStationType.ERiverWater:
                        {
                            //  水位
                            //  解析水位  4(整数位) + 2(小数位)  单位m
                            try
                            {
                                Decimal water = Decimal.Parse(item.Substring(0, 6)) * (Decimal)0.01;
                                data.Water = water;
                            }
                            catch (Exception e)
                            {
                                data.Water = -200;
                            }
                            break;
                        }
                    default: break;
                }
                //解析电压 2（整数位）+ 2（小数位） 单位 V
                try
                {
                    Decimal voltage = Decimal.Parse(item.Substring(10, 4)) * (Decimal)0.01;
                    data.Voltge = voltage;
                }
                catch (Exception e)
                {
                    data.Voltge = -20;
                }
                result.Add(data);
            }
            return result;
        }
        /// <summary>
        /// 单个数据项读取函数
        /// </summary>
        /// <param name="data">原始数据信息</param>
        /// <param name="recvtime">接收时间</param>
        /// <param name="voltage">电压信息</param>
        /// <param name="stationType">站点类型，作为读取数据的依据</param>
        /// <param name="report">作为返回的结果</param>
        /// <returns>填充成功与否</returns>
        private bool FillData(string data, DateTime recvtime, decimal voltage, EStationType stationType, out CReportData report)
        {
            report = new CReportData();
            try
            {
                //时间
                report.Time = recvtime;
                //电压
                if (report.Time.Minute != 0)
                { report.Voltge = 0; }
                else { report.Voltge = voltage; }
                //根据站类读取相应的数据
                switch (stationType)
                {
                    //水位站只要读水位信息
                    case EStationType.ERiverWater:
                        try
                        {
                            Decimal water = Decimal.Parse(data.Substring(0, 6)) * (Decimal)0.01;
                            report.Water = water;
                        }
                        catch (Exception e)
                        {
                            report.Water = -20000;
                        }
                        break;
                    //雨量站只要读雨量信息
                    case EStationType.ERainFall:
                        try
                        {
                            Decimal Rain = Decimal.Parse(data.Substring(6, 4));
                            report.Rain = Rain;
                        }
                        catch (Exception e)
                        {
                            report.Rain = -1;
                        }
                        break;
                    //水文站要读水位信息和雨量信息
                    case EStationType.EHydrology:
                        try
                        {
                            Decimal water = Decimal.Parse(data.Substring(0, 6)) * (Decimal)0.01;
                            report.Water = water;
                        }
                        catch (Exception e)
                        {
                            report.Water = -20000;
                        }
                        try
                        {
                            Decimal Rain = Decimal.Parse(data.Substring(6, 4));
                            report.Rain = Rain;
                        }
                        catch (Exception e)
                        {
                            report.Rain = -1;
                        }
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine("解析中游局信息中站点类别有误！");
                        break;
                }
                return true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return false;
            }
        }

        public bool Parse_1(string msg, out CReportStruct report)
        {
            throw new NotImplementedException();
        }

        public bool Parse_2(string msg, out CReportStruct report)
        {
            throw new NotImplementedException();
        }
    }
}