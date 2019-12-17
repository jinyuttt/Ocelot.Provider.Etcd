#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：Ocelot.Provider.Etcd.Cluster
* 项目描述 ：
* 类 名 称 ：Util
* 类 描 述 ：
* 命名空间 ：Ocelot.Provider.Etcd.Cluster
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion

using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Provider.Etcd.Cluster
{

    /* ============================================================================== 
* 功能描述：Util 
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class Util
    {
        public static string FromGoogleString(ByteString googleString)
        {
            return googleString.ToString(Encoding.Default);
        }

        public static ByteString ToGoogleString(string str)
        {
            return ByteString.CopyFrom(str, Encoding.Default);
        }
    }
}
