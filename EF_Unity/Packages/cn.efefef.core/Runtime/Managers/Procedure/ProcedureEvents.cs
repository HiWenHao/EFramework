/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 18:52:16
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-07 18:52:16
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Managers.Procedure
{
    public struct ProcedureEnterEvent
    {
        public uint Uid;
        public uint ParentUid;
        public Type ProcedureType;
        public int Depth;
    }

    public struct ProcedureActivateEvent
    {
        public uint Uid;
        public Type ProcedureType;
        public int Depth;
    }

    public struct ProcedureSuspendEvent
    {
        public uint Uid;
        public Type ProcedureType;
        public int Depth;
    }

    public struct ProcedureResumeEvent
    {
        public uint Uid;
        public Type ProcedureType;
        public int Depth;
    }

    public struct ProcedureLeaveEvent
    {
        public uint Uid;
        public Type ProcedureType;
        public int Depth;
    }
}