using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GorillaExtensions
{
	public static class GorillaExtensionMethods
	{
		public static T GetComponentInHierarchy<T>(this Scene scene, bool includeInactive = true) where T : Component
		{
			GameObject[] rootGameObjects = scene.GetRootGameObjects();
			foreach (GameObject gameObject in rootGameObjects)
			{
				T component = gameObject.GetComponent<T>();
				if ((Object)component != (Object)null)
				{
					return component;
				}
				Transform[] componentsInChildren = gameObject.GetComponentsInChildren<Transform>(includeInactive);
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					component = componentsInChildren[j].GetComponent<T>();
					if ((Object)component != (Object)null)
					{
						return component;
					}
				}
			}
			return null;
		}

		public static List<T> GetComponentsInHierarchy<T>(this Scene scene, bool includeInactive = true)
		{
			List<T> list = new List<T>();
			GameObject[] rootGameObjects = scene.GetRootGameObjects();
			for (int i = 0; i < rootGameObjects.Length; i++)
			{
				T[] componentsInChildren = rootGameObjects[i].GetComponentsInChildren<T>(includeInactive);
				list.AddRange(componentsInChildren);
			}
			return list;
		}

		public static List<GameObject> GetGameObjectsInHierarchy(this Scene scene, bool includeInactive = true)
		{
			return scene.GetComponentsInHierarchy<GameObject>(includeInactive);
		}

		public static List<T> GetComponentsWithPattern<T>(this Scene scene, string pattern, bool includeInactive = true) where T : Component
		{
			List<T> componentsInHierarchy = scene.GetComponentsInHierarchy<T>(includeInactive);
			List<T> list = new List<T>(componentsInHierarchy.Count);
			foreach (T item in componentsInHierarchy)
			{
				if (Regex.IsMatch(item.transform.name, pattern))
				{
					list.Add(item);
				}
			}
			return list;
		}

		public static List<GameObject> GetGameObjectsWithPattern(this Scene scene, string pattern, bool includeInactive = true)
		{
			List<Transform> componentsWithPattern = scene.GetComponentsWithPattern<Transform>(pattern, includeInactive);
			List<GameObject> list = new List<GameObject>(componentsWithPattern.Count);
			foreach (Transform item in componentsWithPattern)
			{
				list.Add(item.gameObject);
			}
			return list;
		}

		public static List<T> GetComponentsWithPatterns<T>(this Scene scene, string[] patterns, bool includeInactive = true, int maxCount = -1) where T : Component
		{
			List<T> componentsInHierarchy = scene.GetComponentsInHierarchy<T>(includeInactive);
			List<T> list = new List<T>(componentsInHierarchy.Count);
			if (maxCount == 0)
			{
				return list;
			}
			int num = 0;
			foreach (T item in componentsInHierarchy)
			{
				foreach (string pattern in patterns)
				{
					if (Regex.IsMatch(item.name, pattern))
					{
						list.Add(item);
						num++;
						if (maxCount > 0 && num >= maxCount)
						{
							return list;
						}
					}
				}
			}
			return list;
		}

		public static List<T> GetComponentsWithPatterns<T>(this Scene scene, string[] patterns, string[] excludePatterns, bool includeInactive = true, int maxCount = -1) where T : Component
		{
			List<T> componentsInHierarchy = scene.GetComponentsInHierarchy<T>(includeInactive);
			List<T> list = new List<T>(componentsInHierarchy.Count);
			if (maxCount == 0)
			{
				return list;
			}
			int num = 0;
			foreach (T item in componentsInHierarchy)
			{
				bool flag = false;
				foreach (string pattern in patterns)
				{
					if (flag || !Regex.IsMatch(item.name, pattern))
					{
						continue;
					}
					foreach (string pattern2 in excludePatterns)
					{
						if (!flag)
						{
							flag = Regex.IsMatch(item.name, pattern2);
						}
					}
					if (!flag)
					{
						list.Add(item);
						num++;
						if (maxCount > 0 && num >= maxCount)
						{
							return list;
						}
					}
				}
			}
			return list;
		}

		public static List<GameObject> GetGameObjectsWithPatterns(this Scene scene, string[] patterns, bool includeInactive = true, int maxCount = -1)
		{
			List<Transform> componentsWithPatterns = scene.GetComponentsWithPatterns<Transform>(patterns, includeInactive, maxCount);
			List<GameObject> list = new List<GameObject>(componentsWithPatterns.Count);
			foreach (Transform item in componentsWithPatterns)
			{
				list.Add(item.gameObject);
			}
			return list;
		}

		public static List<GameObject> GetGameObjectsWithPatterns(this Scene scene, string[] patterns, string[] excludePatterns, bool includeInactive = true, int maxCount = -1)
		{
			List<Transform> componentsWithPatterns = scene.GetComponentsWithPatterns<Transform>(patterns, excludePatterns, includeInactive, maxCount);
			List<GameObject> list = new List<GameObject>(componentsWithPatterns.Count);
			foreach (Transform item in componentsWithPatterns)
			{
				list.Add(item.gameObject);
			}
			return list;
		}

		public static string GetPath(this Transform transform)
		{
			string text = transform.name;
			while (transform.parent != null)
			{
				transform = transform.parent;
				text = transform.name + "/" + text;
			}
			return "/" + text;
		}

		public static string GetPath(this GameObject gameObject)
		{
			return gameObject.transform.GetPath();
		}

		public static List<GameObject> GetGameObjectsInHierarchy(this Scene scene, string name, bool includeInactive = true)
		{
			List<GameObject> list = new List<GameObject>();
			GameObject[] rootGameObjects = scene.GetRootGameObjects();
			foreach (GameObject gameObject in rootGameObjects)
			{
				if (gameObject.name.Contains(name))
				{
					list.Add(gameObject);
				}
				Transform[] componentsInChildren = gameObject.GetComponentsInChildren<Transform>(includeInactive);
				foreach (Transform transform in componentsInChildren)
				{
					if (transform.name.Contains(name))
					{
						list.Add(transform.gameObject);
					}
				}
			}
			return list;
		}

		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
		{
			T val = gameObject.GetComponent<T>();
			if ((Object)val == (Object)null)
			{
				val = gameObject.AddComponent<T>();
			}
			return val;
		}

		public static void SetLossyScale(this Transform transform, Vector3 scale)
		{
			scale = transform.InverseTransformVector(scale);
			Vector3 lossyScale = transform.lossyScale;
			transform.localScale = new Vector3(scale.x / lossyScale.x, scale.y / lossyScale.y, scale.z / lossyScale.z);
		}
	}
}
