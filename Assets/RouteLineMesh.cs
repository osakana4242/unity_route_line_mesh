using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode]
public class RouteLineMesh : MonoBehaviour
{
	[SerializeField]
	Data data_ = new Data();

	Inner inner_;

	Inner getInner()
	{
		return inner_ ?? (inner_ = new Inner(gameObject));
	}

	void OnDestroy()
	{
		if (inner_ != null) {
			inner_.Dispose();
			inner_ = null;
		}
	}

	void OnValidate()
	{
		getInner().Reflesh(data_);
	}

	void OnEnable()
	{
		getInner().Reflesh(data_);
	}

	[System.Serializable]
	public class Data
	{
		public EdgeItem startEdge_ = new EdgeItem(0.1f);
		public EdgeItem endEdge_ = new EdgeItem(0.1f);
		public List< Vector3 > positions_ = new List<Vector3>() {
			new Vector3(0, 0, 0),
			new Vector3(2, 0, 0),
			new Vector3(4, 2, 0),
			new Vector3(0, 2, 0),
		};
		public float weight = 1f;
		public float scroll_ = 0f;
	}

	[System.Serializable]
	public struct EdgeItem
	{
		public float length;

		public EdgeItem(float length)
		{
			this.length = length;
		}
	}

	class Inner : System.IDisposable
	{
		MeshFilter meshFilter_;
		MeshRenderer meshRenderer_;
		Mesh mesh_;
		VertexHelper vh_;
		float scroll_;

		public Inner(GameObject gameObject)
		{
			meshFilter_ = gameObject.GetComponent<MeshFilter>();
			meshRenderer_ = gameObject.GetComponent<MeshRenderer>();

			mesh_ = new Mesh();
			meshFilter_.sharedMesh = mesh_;
			vh_ = new VertexHelper(mesh_);
		}

		public void Dispose()
		{
			meshRenderer_ = null;
			meshFilter_ = null;

			vh_.Clear();
			vh_ = null;
			Object.DestroyImmediate(mesh_);
			mesh_ = null;
		}

		public float Scroll {
			get {
				return scroll_;
			}
			set {
				scroll_ = value;
				meshRenderer_.material.mainTextureOffset = new Vector2(-scroll_, 0f);
			}
		}

		public void Reflesh(Data data)
		{
			vh_.Clear();
			int vOffset = 0;
			float length = 0f;
			for (int i = 0, count = data.positions_.Count - 1; i < count; ++i) {
				Vector3 p1 = data.positions_[i];
				Vector3 p4 = data.positions_[i + 1];
				Vector3 p2 = p1;
				Vector3 p3 = p4;

				if (i == 0) {
					var vec = p4 - p1;
					var vecLength = vec.magnitude;
					var t = (vecLength == 0)
						? 0f
						: (data.startEdge_.length / vecLength);
					p2 = Vector3.Lerp(p1, p4, t);
				}
				if ((i + 1) == count) {
					var vec = p4 - p1;
					var vecLength = vec.magnitude;
					var t = (vecLength == 0)
						? 1f
						: (1f - (data.endEdge_.length / vecLength));
					p3 = Vector3.Lerp(p1, p4, t);
				}
				if (p1 != p2) {
					addLine(p1, p2, data.weight, 0f, data.weight, ref length, ref vOffset);
				}
				addLine(p2, p3, data.weight, data.weight, data.weight, ref length, ref vOffset);
				if (p3 != p4) {
					addLine(p3, p4, data.weight, data.weight, 0f, ref length, ref vOffset);
				}
			}
			Scroll = data.scroll_;
			vh_.FillMesh(mesh_);
		}

		void addLine(Vector3 p1, Vector3 p2, float baseWeight, float startWeight, float endWeight, ref float length, ref int vOffset)
		{
			Vector3 vec = p2 - p1;
			float startLength = length;
			float endLength = length + vec.magnitude;
			length = endLength;
			// 90度反時計回り.
			var up = new Vector3(-vec.y, vec.x, vec.z);
			var upn = up.normalized;
			var upv1 = upn * startWeight * 0.5f;
			var upv2 = upn * endWeight * 0.5f;
			var a = p1 + upv1;
			var b = p1 - upv1;
			var c = p2 + upv2;
			var d = p2 - upv2;
			//				Debug.Log(string.Format("{0}, {1}, abcd({2} {3} {4} {5})", vec, up, a, b, c, d));
			UIVertex v = new UIVertex();
			v.color = new Color32(255, 255, 255, 255);
			v.position = a;
			v.uv0 = new Vector2(startLength, 0.5f - (0.5f * startWeight / baseWeight));
			vh_.AddVert(v);
			v.position = b;
			v.uv0 = new Vector2(startLength, 0.5f + (0.5f * startWeight / baseWeight));
			vh_.AddVert(v);
			v.position = c;
			v.uv0 = new Vector2(endLength, 0.5f - (0.5f * endWeight / baseWeight));
			vh_.AddVert(v);
			v.position = d;
			v.uv0 = new Vector2(endLength, 0.5f + (0.5f * endWeight / baseWeight));
			vh_.AddVert(v);

			// 0-2
			// | |
			// 1-3

			vh_.AddTriangle(vOffset + 0, vOffset + 2, vOffset + 1);
			vh_.AddTriangle(vOffset + 1, vOffset + 2, vOffset + 3);
			vOffset += 4;
		}
	}
}
