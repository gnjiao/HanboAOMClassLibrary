﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Hanbo.WindowsFormsControlLibrary.Enum;
using System.Reflection;
using App.Common.Views;
using ViewROI;
using HalconDotNet;
using Hanbo.Image.Grab;

namespace Hanbo.WindowsFormsControlLibrary
{
	/// <summary>
	/// 狀態列 User Control
	/// <para>連線狀態, 操作模式, 縮放比例, 灰階, 系統訊息</para>
	/// </summary>
	public partial class HalconStatusStrip : UserControl
	{
		/// <summary>
		/// 系統訊息儲存
		/// </summary>
		private List<string> _messgeStore;

		#region 建構子
		/// <summary>
		/// 建構子
		/// <para>要啟用時，必需叫用 Init 方法</para>
		/// </summary>
		public HalconStatusStrip()
		{
			InitializeComponent();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// 初始化狀態列事件
		/// </summary>
		/// <param name="viewPort"></param>
		/// <param name="viewController"></param>
		public bool Init(HWindowControl viewPort, HWndCtrl viewController, GrabImageWorkingMan camera)
		{
			bool success = true;
			try
			{
				setMessageStore();
				setWatchCoordinate(viewPort);
				setWachtZoomChanged(viewController);
				setWatchOperationModeChange(viewController);
				setWatchGrayLevel(viewPort, viewController);
				setWatchCameraStatus(camera);
			}
			catch (Exception ex)
			{
				success = false;
				Hanbo.Log.LogManager.Error("UC_StatusStrip Error");
				Hanbo.Log.LogManager.Error(ex);
			}
			return success;
		}
		/// <summary>
		/// <para>***************</para>
		/// 設定 Camear 狀態
		/// <para>***************</para>
		/// </summary>
		/// <param name="camera"></param>
		private void setWatchCameraStatus(GrabImageWorkingMan camera)
		{
			if (camera != null)
			{
				camera.GrabImageStopped += (sender, e) =>
				{
					if (e.Cancelled)
					{
						SetStatus(SystemStatusType.ConnectionStatus, Hanbo.Resources.Resource.Disconnected);
					}
				};
				camera.On_GrabImageStatusChanged += (sender, e) =>
				{
					var cameraConnStatus = Hanbo.Resources.Resource.Disconnected;
					var status = e.Status;
					switch (status.Stage)
					{
						case GrabStage.Connected:
						case GrabStage.ContinuouslyGrabbing:
						case GrabStage.Grabbed:
						case GrabStage.Grabbing:
							cameraConnStatus = Hanbo.Resources.Resource.Connected;
							break;
					}
					SetStatus(SystemStatusType.ConnectionStatus, cameraConnStatus);
				};
				camera.GrabImageException += (ex) =>
				{
					var hException = ex as HOperatorException;
					if (hException != null)
					{
						var errorNumber = hException.GetErrorNumber();
						if (errorNumber == 5312)
						{
							MessageBox.Show(Hanbo.Resources.Resource.Message_CameraIsOccupied);
						}
					}
					AddMessage(ex.Message);
					//SetStatus(SystemStatusType.SystemMsg, ex.Message);
				};
			}
		}

		/// <summary>
		/// <para>***********************</para>
		/// 設定狀態列數值
		/// <para>***********************</para>
		/// </summary>
		/// <param name="type">狀態列 Label 類型</param>
		/// <param name="dispText">顯示文字</param>
		public void SetStatus(SystemStatusType type, string dispText)
		{
			switch (type)
			{
				case SystemStatusType.ConnectionStatus:
					this.ConnectionStatus.Text = dispText;
					break;
				case SystemStatusType.ControlMode:
					this.ControlModeStatus.Text = dispText;
					break;
				case SystemStatusType.ZoomFactor:
					this.ZoomFactor.Text = dispText;
					break;
				case SystemStatusType.Coordinate:
					this.ImageCoordinate.Text = dispText;
					break;
				case SystemStatusType.GrayLevel:
					this.GrayLevelLabel.Text = dispText;
					break;
				case SystemStatusType.SystemMsg:
					this.MsgStatus.Text = dispText;
					break;
			}
		}

		/// <summary>
		/// 加入訊息
		/// </summary>
		/// <param name="msg"></param>
		public void AddMessage(string msg)
		{
			_messgeStore.Add(msg);
			SetStatus(SystemStatusType.SystemMsg, _messgeStore.Count.ToString());
		}
		#endregion

		#region private methods

		/// <summary>
		/// <para>*******</para>
		/// 設定訊息儲存
		/// <para>*******</para>
		/// </summary>
		private void setMessageStore()
		{
			_messgeStore = new List<string>();
			this.MsgStatus.Click -= messageStatus_Click;
			this.MsgStatus.Click += messageStatus_Click;
		}

		/// <summary>
		/// <para>***************************************</para>
		/// Watch OperationMode Change
		/// <para>觀察影像操作模式 (移動, 放大/縮小, 放大鏡)</para>
		/// <para>***************************************</para>
		/// </summary>
		/// <param name="viewController"></param>
		private void setWatchOperationModeChange(HWndCtrl viewController)
		{
			if (viewController != null)
			{
				viewController.On_OperationModeChanged += (mode) =>
				{
					var modeText = Hanbo.Resources.Resource.ResourceManager.GetString(mode);
					SetStatus(SystemStatusType.ControlMode, modeText);
				};

			}
		}

		/// <summary>
		/// <para>*****************</para>
		/// Watch Coordniate (座標)
		/// <para>*****************</para>
		/// </summary>
		/// <param name="viewPort"></param>
		private void setWatchCoordinate(HWindowControl viewPort)
		{
			if (viewPort != null)
			{
				viewPort.HMouseMove += (sender, e) =>
				{
					var coordinateText = String.Format("{0}, {1}", Math.Round(e.X, 2), Math.Round(e.Y, 2));
					SetStatus(SystemStatusType.Coordinate, coordinateText);
				};
			}
		}

		/// <summary>
		/// <para>******************</para>
		/// Watch Gray Level (灰階
		/// <para>******************</para>
		/// </summary>
		/// <param name="viewPort"></param>
		private void setWatchGrayLevel(HWindowControl viewPort, HWndCtrl viewController)
		{
			if (viewPort != null)
			{
				viewPort.HMouseMove += (sender, e) =>
				{
					if (viewController == null) return;
					var currImage = viewController.GetLastHImage();
					if (currImage != null)
					{
						var row = (int)Math.Round(e.Y, 0);
						var col = (int)Math.Round(e.X, 0);
						try
						{
							HTuple width, height, grayval;
							currImage.GetImageSize(out width, out height);
							var mouseInImage = (row <= height.I && col <= width.I && row >= 0 && col >= 0);
							if (mouseInImage)
							{
								HOperatorSet.GetGrayval(currImage, row, col, out grayval);
								var dispText = grayval.I.ToString();
								SetStatus(SystemStatusType.GrayLevel, dispText);
							}
						}
						catch (Exception ex)
						{
							AddMessage(ex.Message);
						}
					}

				};
			}
		}

		/// <summary>
		/// <para>***********************</para>
		/// Zoom Change Observer (縮放)
		/// <para>***********************</para>
		/// </summary>
		/// <param name="viewController"></param>
		private void setWachtZoomChanged(HWndCtrl viewController)
		{
			if (viewController != null)
			{
				viewController.OnZoomChanged += (zoomFactor) =>
				{
					var rate = Math.Round(100 * 1.0 / zoomFactor, 2);
					var dispText = String.Format("{0}%", rate);
					SetStatus(SystemStatusType.ZoomFactor, dispText);
				};
			}
		}

		/// <summary>
		/// 點選訊息狀態
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void messageStatus_Click(object sender, EventArgs e)
		{
			var msgForm = new StatusForm(_messgeStore) { Text = "系統訊息 (System Messages)" };
			msgForm.ShowDialog();
			SetStatus(SystemStatusType.SystemMsg, _messgeStore.Count.ToString());
		}
		#endregion
	}
}
