﻿namespace Kurisu.EFSharding.Core;

internal enum ConnectionModeEnum
{
    //内存限制模式最小化内存聚合 流式聚合 同时会有多个链接
    MemoryStrictly,

    //CONNECTION_STRICTLY
    //连接数限制模式最小化并发连接数 内存聚合 连接数会有限制
    ConnectionStrictly,

    //系统自动选择内存还是流式聚合
    Auto
}