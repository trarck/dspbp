using System.Collections.Generic;
using UnityEngine;

namespace YH.MyInput
{
	public class Key
	{
		public KeyCode code;
		public bool down;
		public bool up;
		public bool press;

		public int downTime;
		public int upTime;
		public int pressTime;

		public bool useable;
		public bool ignoreActiveTime;

		public Key(KeyCode code)
		{
			this.code = code;
			this.useable = true;
			this.ignoreActiveTime = false;
		}

		public void SetSpecail()
		{
			useable = false;
			ignoreActiveTime = true;
		}

		public void Reset()
		{
			down = false;
			up = false;
			press = false;
			downTime = 0;
			upTime = 0;
			pressTime = 0;
		}

		public void SetDown(int time)
		{
			Reset();
			down = true;
			downTime = time;
		}

		public void SetUp(int time)
		{
			Reset();
			up = true;
			upTime = time;
			press = true;
			pressTime = time;
		}

		public void Use()
		{
			if (useable)
			{
				Reset();
			}
		}

		public void UseDown()
		{
			if (useable)
			{
				down = false;
				downTime = 0;
			}
		}

		public void UseUp()
		{
			if (useable)
			{
				up = false;
				upTime = 0;
			}
		}

		public void UsePress()
		{
			if (useable)
			{
				press = false;
				pressTime = 0;
			}
		}
	}

	public class MultiKey
	{
		public string name;
		public bool autoUse;

		protected List<KeyCode> keyCodes = new List<KeyCode>();

		public MultiKey()
		{

		}

		public MultiKey(string name, params KeyCode[] codes)
		{
			this.name = name;
			if (codes.Length > 0)
			{
				keyCodes.AddRange(codes);
			}
			autoUse = false;
		}

		public MultiKey(string name, bool autoUse, params KeyCode[] codes)
		{
			this.name = name;
			if (codes.Length > 0)
			{
				keyCodes.AddRange(codes);
			}
			this.autoUse = autoUse;
		}

		public virtual bool IsDown()
		{
			bool ret = false;
			foreach (var code in keyCodes)
			{
				if (!KeyManager.Instance.IsKeyDown(code))
				{
					return false;
				}
				ret = true;
			}

			if (ret && autoUse)
			{
				foreach (var code in keyCodes)
				{
					KeyManager.Instance.UseKey(code);
				}
			}

			return ret;
		}

		public virtual bool IsUp()
		{
			bool ret = false;
			foreach (var code in keyCodes)
			{
				if (!KeyManager.Instance.IsKeyUp(code))
				{
					return false;
				}
				ret = true;
			}
			if (ret && autoUse)
			{
				foreach (var code in keyCodes)
				{
					KeyManager.Instance.UseKeyUp(code);
				}
			}
			return ret;
		}

		public virtual bool IsPress()
		{
			bool ret = false;
			foreach (var code in keyCodes)
			{
				if (!KeyManager.Instance.IsKeyPress(code))
				{
					return false;
				}
				ret = true;
			}
			if (ret && autoUse)
			{
				foreach (var code in keyCodes)
				{
					KeyManager.Instance.UseKeyPress(code);
				}
			}
			return ret;
		}

		public virtual void Use()
		{
			foreach (var keycode in keyCodes)
			{
				KeyManager.Instance.UseKey(keycode);
			}
		}
	}

	public class CombineKey : MultiKey
	{
		List<KeyCode> specailCodes = new List<KeyCode>();

		public CombineKey(string name, params KeyCode[] codes) : this(name, false, codes)
		{

		}

		public CombineKey(string name, bool autoUse, params KeyCode[] codes)
		{
			this.name = name;
			this.autoUse = autoUse;

			if (codes.Length > 0)
			{
				foreach (var keyCode in codes)
				{
					if (KeyManager.Instance.IsSpecailKey(keyCode))
					{
						specailCodes.Add(keyCode);
					}
					else
					{
						keyCodes.Add(keyCode);
					}
				}
			}
		}

		public override bool IsDown()
		{
			bool ret = true;

			//只要一个符合就可以
			foreach (var code in specailCodes)
			{
				if (KeyManager.Instance.IsKeyDown(code))
				{
					ret = true;
					break;
				}
				else
				{
					ret = false;
				}
			}

			if (ret)
			{
				return base.IsDown();
				////每一个都要符合
				//foreach (var code in keyCodes)
				//{
				//	if (!KeyManager.Instance.IsKeyDown(code))
				//	{
				//		return false;
				//	}
				//}

				//if (autoUse)
				//{
				//	foreach (var code in keyCodes)
				//	{
				//		KeyManager.Instance.UseKey(code);
				//	}
				//}
			}




			return ret;
		}

		//public bool IsUp()
		//{
		//	bool ret = false;
		//	//只要普通的全部up就行。不用管特殊的
		//	foreach (var code in keyCodes)
		//	{
		//		if (!KeyManager.Instance.IsKeyUp(code))
		//		{
		//			return false;
		//		}
		//		ret = true;
		//	}

		//	if (ret && autoUse)
		//	{
		//		foreach (var code in keyCodes)
		//		{
		//			KeyManager.Instance.UseKeyUp(code);
		//		}
		//	}
		//	return ret;
		//}

		//public bool IsPress()
		//{
		//	bool ret = false;
		//	foreach (var code in keyCodes)
		//	{
		//		if (!KeyManager.Instance.IsKeyPress(code))
		//		{
		//			return false;
		//		}
		//		ret = true;
		//	}

		//	if (ret && autoUse)
		//	{
		//		foreach (var code in keyCodes)
		//		{
		//			KeyManager.Instance.UseKeyPress(code);
		//		}
		//	}
		//	return ret;
		//}

		//public void Use()
		//{
		//	foreach (var keycode in keyCodes)
		//	{
		//		KeyManager.Instance.UseKey(keycode);
		//	}
		//}
	}

	public class KeyManager
	{
		public Dictionary<int, Key> keys = new Dictionary<int, Key>();
		public Dictionary<int, Key> specialKeys = new Dictionary<int, Key>();

		public int keyActiveTime = 1;

		private static KeyManager _instance;

		public static KeyManager Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new KeyManager();
					_instance.Init();
				}
				return _instance;
			}
		}


		public KeyManager()
		{

		}

		#region Init
		public void Init()
		{
			InitSpecailKeys();
		}
		public void InitSpecailKeys()
		{
			CreateSpecialKey(KeyCode.LeftControl);
			CreateSpecialKey(KeyCode.RightControl);
			CreateSpecialKey(KeyCode.LeftShift);
			CreateSpecialKey(KeyCode.RightShift);
			CreateSpecialKey(KeyCode.LeftAlt);
			CreateSpecialKey(KeyCode.RightAlt);
		}
		#endregion

		#region Key
		public Key GetKey(KeyCode code)
		{
			Key key = null;
			int k = (int)code;
			keys.TryGetValue(k, out key);
			return key;
		}

		public Key CreateKey(KeyCode code)
		{
			Key key = null;
			int k = (int)code;
			if (!keys.TryGetValue(k, out key))
			{
				key = new Key(code);
				keys[k] = key;
			}
			return key;
		}

		public Key GetSpecailKey(KeyCode code)
		{
			Key key = null;
			int k = (int)code;
			specialKeys.TryGetValue(k, out key);
			return key;
		}

		public Key CreateSpecialKey(KeyCode code)
		{
			Key key = null;
			int k = (int)code;
			if (!specialKeys.TryGetValue(k, out key))
			{
				key = CreateKey(code);
				specialKeys[k] = key;
			}
			else
			{
				//add to normal if not exists
				CreateKey(code);
			}
			key.useable = false;
			key.ignoreActiveTime = true;
			return key;
		}

		public bool IsSpecailKey(KeyCode code)
		{
			return GetSpecailKey(code) != null;
		}

		public bool IsKeyDown(KeyCode code)
		{
			Key key = CreateKey(code);
			//Debug.LogFormat("Key:{0},{1}.{2}", code, key.down, key.downTime);
			if (key.down)
			{
				return key.ignoreActiveTime || Time.frameCount - key.downTime <= keyActiveTime;
			}
			return false;
		}

		public bool IsKeyUp(KeyCode code)
		{
			Key key = CreateKey(code);
			if (key.up)
			{
				return Time.frameCount - key.upTime <= keyActiveTime;
			}
			return false;
		}

		public bool IsKeyPress(KeyCode code)
		{
			Key key = CreateKey(code);
			if (key.press)
			{
				return Time.frameCount - key.pressTime <= keyActiveTime;
			}
			return false;
		}

		public void UseKey(KeyCode code)
		{
			Key key = GetKey(code);
			if (key != null)
			{
				key.Use();
			}
		}

		public void UseKeyDown(KeyCode code)
		{
			Key key = GetKey(code);
			if (key != null)
			{
				key.UseDown();
			}
		}

		public void UseKeyUp(KeyCode code)
		{
			Key key = GetKey(code);
			if (key != null)
			{
				key.UseUp();
			}
		}

		public void UseKeyPress(KeyCode code)
		{
			Key key = GetKey(code);
			if (key != null)
			{
				key.UsePress();
			}
		}

		#endregion

		public void Update()
		{
			int frame = Time.frameCount;

			foreach (var iter in keys)
			{
				Key key = iter.Value;

				if (UnityEngine.Input.GetKeyDown(key.code))
				{
					key.SetDown(frame);
				}
				else if (UnityEngine.Input.GetKeyUp(iter.Value.code))
				{
					key.SetUp(frame);
				}
			}
		}

	}
}
