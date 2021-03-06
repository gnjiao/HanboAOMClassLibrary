﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;

namespace Hanbo.Device.SingleInstance
{
	public class DeviceChecker
	{
        public static DeviceCheckResult LineScanCheck()
        {
            DeviceCheckResult result = new DeviceCheckResult() { Success = false, Message = "", ExceptionDetail = null, Name = "LineScanCheck" };
            try
            {
                //var imageHandle = DeviceController.GetLineScanInstance();
                //imageHandle.Connect();
                //result.Success = imageHandle.Connected;
                //if (!imageHandle.Connected)
                //    result.Message = Hanbo.Resources.Resource.CameraNotReadyMessage;
                //DeviceController.ReleaseGrabImageWorkingManInstance();
            }
            catch (Exception ex)
            {
                result.ExceptionDetail = ex;
            }
            return result;
        }

		public static DeviceCheckResult CCDCheck()
		{
			DeviceCheckResult result = new DeviceCheckResult() { Success = false, Message = "", ExceptionDetail = null, Name = "CCDCheck" };
			try
			{
				var imageHandle = DeviceController.GetGrabImageWorkingManInstance();
				imageHandle.Connect();
				result.Success = imageHandle.Connected;
				if (!imageHandle.Connected)
					result.Message = Hanbo.Resources.Resource.CameraNotReadyMessage;
				DeviceController.ReleaseGrabImageWorkingManInstance();
			}
			catch (Exception ex)
			{
				result.ExceptionDetail = ex;
				Hanbo.Log.LogManager.Error(ex);
			}
			return result;
		}

		public static DeviceCheckResult DBCheck()
		{
			DeviceCheckResult result = new DeviceCheckResult() { Success = false, Message = "", ExceptionDetail = null, Name = "DBCheck" };
			try
			{
				var connString = ConfigurationManager.ConnectionStrings["SDMSConnString"].ConnectionString;
				using (SqlConnection conn = new SqlConnection(connString))
				{
					conn.Open();
					result.Success = (conn.State == global::System.Data.ConnectionState.Open);
				}
			}
			catch (SqlException ex)
			{
				result.Message = ex.Message;
				result.ExceptionDetail = ex;
			}
			return result;
		}

		public static DeviceCheckResult LightControlCheck()
		{
			DeviceCheckResult result = new DeviceCheckResult() { Success = false, Message = "", ExceptionDetail = null, Name = "LightControlCheck" };
			try
			{
				var lightControl = DeviceController.GetCCSLightControlManagerInstance();
				result.Success = !lightControl.Connected;
				if (!lightControl.Connected)
				{
					result.Message = lightControl.TestConnect(1500);
					Thread.Sleep(2000);
					result.Success = result.Message == "OK";
				}
				DeviceController.ReleaseCCSLightControlManagerInstance();
			}
			catch (Exception ex)
			{
				result.Message = ex.Message;
				result.ExceptionDetail = ex;
			}
			return result;
		}
	}
}
