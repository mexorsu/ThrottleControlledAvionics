﻿/* Name: Throttle Controlled Avionics, Fork by Allis Tauri
 *
 * Authors: Quinten Feys & Willem van Vliet & Allis Tauri
 * License: BY: Attribution-ShareAlike 3.0 Unported (CC BY-SA 3.0): 
 * http://creativecommons.org/licenses/by-sa/3.0/
 * 
 */

using System;
using UnityEngine;
using System.IO;

namespace ThrottleControlledAvionics
{
	public static class Extensions
	{
		#region Resources
		const string ElectricChargeName = "ElectricCharge";
		static PartResourceDefinition _electric_charge;

		public static PartResourceDefinition ElectricCharge
		{ 
			get
			{ 
				if(_electric_charge == null)
					_electric_charge = PartResourceLibrary.Instance.GetDefinition(ElectricChargeName);
				return _electric_charge;
			} 
		}
		#endregion

		#region Logging
		public static string Title(this Part p) { return p.partInfo != null? p.partInfo.title : p.name; }

		public static void Log(this Part p, string msg, params object[] args)
		{
			var vname = p.vessel == null? "" : p.vessel.vesselName;
			var _msg = string.Format("{0}.{1} [{2}]: {3}", 
			                             vname, p.name, p.flightID, msg);
			Utils.Log(_msg, args);
		}
		#endregion

		public static bool ElectricChargeAvailible(this Vessel v)
		{
			var ec = v.GetActiveResource(ElectricCharge);
			return ec != null && ec.amount > 0;
		}
	}

	public static class Utils
	{
		public static void writeToFile(String text)
		{
			using(var writer = new StreamWriter("DumpFile.txt", true))
				writer.Write(text + " \n");
		}

		public static string formatVector(Vector3 v)
		{ return string.Format("({0}, {1}, {2}); |v| = {3}", v.x, v.y, v.z, v.magnitude); }

		public static string formatVector(Vector3d v)
		{ return string.Format("({0}, {1}, {2}); |v| = {3}", v.x, v.y, v.z, v.magnitude); }

		public static void Log(string msg, params object[] args)
		{ 
			for(int i = 0; i < args.Length; i++) 
			{
				if(args[i] is Vector3) args[i] = formatVector((Vector3)args[i]);
				else if(args[i] is Vector3d) args[i] = formatVector((Vector3d)args[i]);
				else if(args[i] == null) args[i] = "null";
			}
			Debug.Log(string.Format("[TCA] "+msg, args)); 
		}

		public static float Asymptote01(float x, float k=1) { return 1-1/(x/k+1); }
		public static float ClampL(float x, float low)  { return x < low  ? low  : x;  }
		public static float ClampH(float x, float high) { return x < high ? high : x;  }

		public static void CheckRect(ref Rect R)
		{
			//check size
			if(R.width > Screen.width) R.width = Screen.width;
			if(R.height > Screen.height) R.height = Screen.height;
			//check position
			if(R.xMin < 0) R.x -= R.xMin;
			else if(R.xMax > Screen.width) R.x -= R.xMax-Screen.width;
			if(R.yMin < 0) R.y -= R.yMin;
			else if(R.yMax > Screen.height) R.y -= R.yMax-Screen.height;
		}
	}

	public class ConfigNodeObject : IConfigNode
	{
		public const string NODE_NAME = "NODE";

		virtual public void Load(ConfigNode node)
		{ ConfigNode.LoadObjectFromConfig(this, node); }

		virtual public void Save(ConfigNode node)
		{ ConfigNode.CreateConfigFromObject(this, node); }
	}

	public abstract class PI_Controller<T> : ConfigNodeObject
	{
		new public const string NODE_NAME = "PI";
		protected T value = default(T);
		protected T integral_error = default(T);

		//coefficients
		[Persistent] public float P, I;

		protected PI_Controller() : this(0.8f, 0.3f) {}
		protected PI_Controller(float P, float I)
		{ this.P = P; this.I = I; }

		public abstract void Update(T new_value);

		//access
		public T Value { get { return value; } }
		public static implicit operator T(PI_Controller<T> c)
		{ return c.value; }
	}

	public class PIv_Controller : PI_Controller<Vector3>
	{
		public PIv_Controller() {}
		public PIv_Controller(float P, float I) : base(P, I) {}

		public override void Update(Vector3 new_value)
		{
			var error = new_value - value;
			integral_error += error * TimeWarp.fixedDeltaTime;
			value = new_value * P + integral_error * I;
		}
	}
}