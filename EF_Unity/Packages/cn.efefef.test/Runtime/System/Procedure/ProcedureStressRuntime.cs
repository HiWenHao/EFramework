/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-09 18:59:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-09 18:59:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEngine;

public static class ProcedureStressRuntime
{
	public static int CurrentAlive;
	
	public static int EnterCount;

	public static int LeaveCount;

	public static int ActiveCount;

	public static int ExceptionCount;

	public static int TimeoutSimulation;

	public static int MaxAlive;

	public static float StartTime;

	public static void Reset()
	{
		EnterCount = 0;
		LeaveCount = 0;
		ActiveCount = 0;
		ExceptionCount = 0;
		TimeoutSimulation = 0;
		MaxAlive = 0;

		StartTime = Time.realtimeSinceStartup;
	}
}