using System;
using HalconDotNet;
using ViewROI;
using System.Collections;
using ViewROI.Interface;



namespace ViewROI
{

	public delegate void FuncROIDelegate();

	/// <summary>
	/// This class creates and manages ROI objects. It responds 
	/// to  mouse device inputs using the methods mouseDownAction and 
	/// mouseMoveAction. You don't have to know this class in detail when you 
	/// build your own C# project. But you must consider a few things if 
	/// you want to use interactive ROIs in your application: There is a
	/// quite close connection between the ROIController and the HWndCtrl 
	/// class, which means that you must 'register' the ROIController
	/// with the HWndCtrl, so the HWndCtrl knows it has to forward user input
	/// (like mouse events) to the ROIController class.  
	/// The visualization and manipulation of the ROI objects is done 
	/// by the ROIController.
	/// This class provides special support for the matching
	/// applications by calculating a model region from the list of ROIs. For
	/// this, ROIs are added and subtracted according to their sign.
	/// </summary>
	public class ROIController
	{

		/// <summary>
		/// Constant for setting the ROI mode: positive ROI sign.
		/// </summary>
		public const int MODE_ROI_POS = 21;

		/// <summary>
		/// Constant for setting the ROI mode: negative ROI sign.
		/// </summary>
		public const int MODE_ROI_NEG = 22;

		/// <summary>
		/// Constant for setting the ROI mode: no model region is computed as
		/// the sum of all ROI objects.
		/// </summary>
		public const int MODE_ROI_NONE = 23;

		/// <summary>Constant describing an update of the model region</summary>
		public const int EVENT_UPDATE_ROI = 50;

		public const int EVENT_CHANGED_ROI_SIGN = 51;

		/// <summary>Constant describing an update of the model region</summary>
		public const int EVENT_MOVING_ROI = 52;

		public const int EVENT_DELETED_ACTROI = 53;

		public const int EVENT_DELETED_ALL_ROIS = 54;

		public const int EVENT_ACTIVATED_ROI = 55;

		public const int EVENT_CREATED_ROI = 56;

		public const int EVENT_Reload_ROI = 90;


		private ROI roiMode;
		private int stateROI;
		private double currX, currY;


		/// <summary>Index of the active ROI object</summary>
		public int activeROIidx;
		public int deletedIdx;

		/// <summary>List containing all created ROI objects so far</summary>
		public ArrayList ROIList;

		/// <summary>
		/// Region obtained by summing up all negative 
		/// and positive ROI objects from the ROIList 
		/// </summary>
		public HRegion ModelROI;

		private string activeCol = "green";
		private string activeHdlCol = "red";
		private string inactiveCol = "yellow";

		/// <summary>
		/// Reference to the HWndCtrl, the ROI Controller is registered to
		/// </summary>
		public HWndCtrl viewController;

		/// <summary>
		/// Delegate that notifies about changes made in the model region
		/// </summary>
		public IconicDelegate NotifyRCObserver;

		private ROI _waitForClickROI;

		/// <summary>Constructor</summary>
		public ROIController()
		{
			stateROI = MODE_ROI_NONE;
			ROIList = new ArrayList();
			activeROIidx = -1;
			ModelROI = new HRegion();
			NotifyRCObserver = new IconicDelegate(dummyI);
			deletedIdx = -1;
			currX = currY = -1;
		}
		private double _Epsilon = 35.0;			//maximal shortest distance to one of the handles

		/// <summary>
		/// maximal shortest distance to one of the handles
		/// </summary>
		public void SetEpsilon(double epison)
		{
			this._Epsilon = epison;
		}

		/// <summary>Registers the HWndCtrl to this ROIController instance</summary>
		public void setViewController(HWndCtrl view)
		{
			viewController = view;
			if (viewController != null)
			{
				viewController.OnZoomChanged -= viewController_OnZoomChanged;
				viewController.OnZoomChanged += viewController_OnZoomChanged;
			}
		}

		void viewController_OnZoomChanged(double zoomFactor)
		{
			this.SetZoomFactor(zoomFactor);
		}

		/// <summary>Gets the ModelROI object</summary>
		public HRegion getModelRegion()
		{
			return ModelROI;
		}

		/// <summary>Gets the List of ROIs created so far</summary>
		public ArrayList getROIList()
		{
			return ROIList;
		}

		/// <summary>Get the active ROI</summary>
		public ROI getActiveROI()
		{
			if (activeROIidx != -1)
				return ((ROI)ROIList[activeROIidx]);

			return null;
		}

		public int getActiveROIIdx()
		{
			return activeROIidx;
		}

		public void setActiveROIIdx(int active)
		{
			activeROIidx = active;
		}
		public void SetActiveROI(ROI r)
		{
			for (int roiIndex = 0; roiIndex < ROIList.Count; roiIndex++)
			{
				var roi = (ROI)ROIList[roiIndex];
				if (roi.ID == r.ID)
				{
					setActiveROIIdx(roiIndex);
					break;
				}
			}
		}

		public int getDelROIIdx()
		{
			return deletedIdx;
		}

		/// <summary>
		/// To create a new ROI object the application class initializes a 
		/// 'seed' ROI instance and passes it to the ROIController.
		/// The ROIController now responds by manipulating this new ROI
		/// instance.
		/// </summary>
		/// <param name="r">
		/// 'Seed' ROI object forwarded by the application forms class.
		/// </param>
		public void setROIShape(ROI r)
		{
			roiMode = r;
			roiMode.setOperatorFlag(stateROI);
		}


		/// <summary>
		/// Sets the sign of a ROI object to the value 'mode' (MODE_ROI_NONE,
		/// MODE_ROI_POS,MODE_ROI_NEG)
		/// </summary>
		public void setROISign(int mode)
		{
			stateROI = mode;

			if (activeROIidx != -1)
			{
				((ROI)ROIList[activeROIidx]).setOperatorFlag(stateROI);
				viewController.repaint();
				NotifyRCObserver(ROIController.EVENT_CHANGED_ROI_SIGN);
			}
		}

		/// <summary>
		/// Removes the ROI object that is marked as active. 
		/// If no ROI object is active, then nothing happens. 
		/// </summary>
		public void removeActive()
		{
			if (activeROIidx != -1)
			{
				ROIList.RemoveAt(activeROIidx);
				deletedIdx = activeROIidx;
				activeROIidx = -1;
				viewController.repaint();
				NotifyRCObserver(EVENT_DELETED_ACTROI);
			}
		}
		public void RemoveROI(int index)
		{
			setActiveROIIdx(index);
			removeActive();
		}

		public void RemoveROI(string roiID)
		{
			for (int roiIndex = 0; roiIndex < ROIList.Count; roiIndex++)
			{
				var roi = (ROI)ROIList[roiIndex];
				if (roi.ID == roiID)
				{
					RemoveROI(roiIndex);
					break;
				}
			}
		}


		/// <summary>
		/// Calculates the ModelROI region for all objects contained 
		/// in ROIList, by adding and subtracting the positive and 
		/// negative ROI objects.
		/// </summary>
		public bool defineModelROI()
		{
			HRegion tmpAdd, tmpDiff, tmp;
			double row, col;

			if (stateROI == MODE_ROI_NONE)
				return true;

			tmpAdd = new HRegion();
			tmpDiff = new HRegion();
			tmpAdd.GenEmptyRegion();
			tmpDiff.GenEmptyRegion();

			for (int i = 0; i < ROIList.Count; i++)
			{
				switch (((ROI)ROIList[i]).getOperatorFlag())
				{
					case ROI.POSITIVE_FLAG:
						tmp = ((ROI)ROIList[i]).getRegion();
						tmpAdd = tmp.Union2(tmpAdd);
						break;
					case ROI.NEGATIVE_FLAG:
						tmp = ((ROI)ROIList[i]).getRegion();
						tmpDiff = tmp.Union2(tmpDiff);
						break;
					default:
						break;
				}//end of switch
			}//end of for

			ModelROI = null;

			if (tmpAdd.AreaCenter(out row, out col) > 0)
			{
				tmp = tmpAdd.Difference(tmpDiff);
				if (tmp.AreaCenter(out row, out col) > 0)
					ModelROI = tmp;
			}

			//in case the set of positiv and negative ROIs dissolve 
			if (ModelROI == null || ROIList.Count == 0)
				return false;

			return true;
		}


		/// <summary>
		/// Clears all variables managing ROI objects
		/// </summary>
		public void reset()
		{
			ROIList.Clear();
			activeROIidx = -1;
			ModelROI = null;
			roiMode = null;
			NotifyRCObserver(EVENT_DELETED_ALL_ROIS);
		}


		/// <summary>
		/// Deletes this ROI instance if a 'seed' ROI object has been passed
		/// to the ROIController by the application class.
		/// 
		/// </summary>
		public void resetROI()
		{
			activeROIidx = -1;
			roiMode = null;
		}

		/// <summary>Defines the colors for the ROI objects</summary>
		/// <param name="aColor">Color for the active ROI object</param>
		/// <param name="inaColor">Color for the inactive ROI objects</param>
		/// <param name="aHdlColor">
		/// Color for the active handle of the active ROI object
		/// </param>
		public void setDrawColor(string aColor,
								   string aHdlColor,
								   string inaColor)
		{
			if (aColor != "")
				activeCol = aColor;
			if (aHdlColor != "")
				activeHdlCol = aHdlColor;
			if (inaColor != "")
				inactiveCol = inaColor;
		}


		/// <summary>
		/// Paints all objects from the ROIList into the HALCON window
		/// </summary>
		/// <param name="window">HALCON window</param>
		public void paintData(HalconDotNet.HWindow window)
		{
			window.SetDraw("margin");
			window.SetLineWidth(1);
			//畫還未完成的 ROI
			if (_waitForClickROI != null)
			{
				_waitForClickROI.draw(window);
			}

			if (ROIList.Count > 0)
			{
				window.SetColor(inactiveCol);
				window.SetDraw("margin");

				//畫所有的 ROI				
				for (int i = 0; i < ROIList.Count; i++)
				{
					if (i == activeROIidx) continue;
					var roi = (ROI)ROIList[i];
					roi.IsActive = false;
					if (roi.Visiable)
					{
						window.SetLineStyle(roi.flagLineStyle);
						roi.SetZoomRatio(_zoomFactor);
						roi.draw(window);
					}
				}

				//畫 Active ROI
				if (activeROIidx != -1)
				{
					var activeROI = (ROI)ROIList[activeROIidx];
					activeROI.IsActive = true;
					if (activeROI.Visiable)
					{
						window.SetColor(activeCol);
						window.SetLineStyle(activeROI.flagLineStyle);
						activeROI.draw(window);

						window.SetColor(activeHdlCol);
						activeROI.displayActive(window);
					}
				}
			}
		}

		/// <summary>
		/// Reaction of ROI objects to the 'mouse button down' event: changing
		/// the shape of the ROI and adding it to the ROIList if it is a 'seed'
		/// ROI.
		/// </summary>
		/// <param name="imgX">x coordinate of mouse event</param>
		/// <param name="imgY">y coordinate of mouse event</param>
		/// <returns></returns>
		public int mouseDownAction(double imgX, double imgY)
		{
			int idxROI = -1;
			double max = 10000, dist = 0;

			if (roiMode != null)			 //either a new ROI object is created
			{
				var smartROI = roiMode as IContinueZoom;
				if (smartROI != null)
				{
					doContinueZoomROIAction(imgX, imgY, smartROI);
				}
				else
				{
					doNormalROIAction(imgX, imgY);
				}
			}
			else if (ROIList.Count > 0)		// ... or an existing one is manipulated
			{
				activeROIidx = -1;

				for (int i = 0; i < ROIList.Count; i++)
				{
					var currentROI = ((ROI)ROIList[i]);
					if (currentROI.Visiable)
					{
						dist = currentROI.distToClosestHandle(imgX, imgY);
						if ((dist < max) && (dist < _Epsilon))
						{
							max = dist;
							idxROI = i;
						}
					}

				}//end of for

				if (idxROI >= 0)
				{
					activeROIidx = idxROI;
					NotifyRCObserver(ROIController.EVENT_ACTIVATED_ROI);
				}

				viewController.repaint();
			}
			return activeROIidx;
		}

		private void doNormalROIAction(double imgX, double imgY)
		{
			roiMode.createROI(imgX, imgY);
			ROIList.Add(roiMode);
			roiMode = null;
			activeROIidx = ROIList.Count - 1;
			viewController.repaint();

			NotifyRCObserver(ROIController.EVENT_CREATED_ROI);
		}

		private void doContinueZoomROIAction(double imgX, double imgY, IContinueZoom smartROI)
		{
			var done = smartROI.WaitForClickPoints(imgX, imgY);
			if (smartROI.ClickedPoints == 1)
			{
				_waitForClickROI = (ROI)smartROI;
			}
			viewController.repaint();
			if (done)
			{
				ROIList.Add(smartROI);
				_waitForClickROI = null;
				roiMode = null;
				viewController.DisableZoomContinue();
				activeROIidx = ROIList.Count - 1;
				NotifyRCObserver(ROIController.EVENT_CREATED_ROI);
			}

		}

		/// <summary>
		/// Reaction of ROI objects to the 'mouse button move' event: moving
		/// the active ROI.
		/// </summary>
		/// <param name="newX">x coordinate of mouse event</param>
		/// <param name="newY">y coordinate of mouse event</param>
		public void mouseMoveAction(double newX, double newY)
		{
			if ((newX == currX) && (newY == currY))
				return;

			var roi = ((ROI)ROIList[activeROIidx]);
			if (roi.Visiable)
			{
				roi.moveByHandle(newX, newY);
			}
			viewController.repaint();
			currX = newX;
			currY = newY;
			NotifyRCObserver(ROIController.EVENT_MOVING_ROI);
		}


		/***********************************************************/
		public void dummyI(int v)
		{
		}

		/// <summary>
		/// CreateROIContour, 直接建立 ROI Contour 在 window 上
		/// </summary>
		/// <param name="contour"></param>
		public void CreateROIContour(ROI contour)
		{
			ROIList.Add(contour);
			activeROIidx = ROIList.Count - 1;
			viewController.repaint();
			NotifyRCObserver(ROIController.EVENT_CREATED_ROI);
		}

		/// <summary>
		/// 將 ROI 建立在 Windows 上，並觸發 Update
		/// </summary>
		/// <param name="contour"></param>
		public void ReloadROI(ROI contour)
		{
			ROIList.Add(contour);
			activeROIidx = ROIList.Count - 1;
			if (viewController != null)
				viewController.repaint();

			NotifyRCObserver(ROIController.EVENT_Reload_ROI);
		}

		private double _zoomFactor = 1.0;
		public void SetZoomFactor(double zoomFactor)
		{
			_zoomFactor = zoomFactor;
		}
	}//end of class
}//end of namespace
