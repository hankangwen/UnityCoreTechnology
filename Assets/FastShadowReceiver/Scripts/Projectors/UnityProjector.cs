//
// UnityProjector.cs
//
// Fast Shadow Receiver
//
// Copyright 2014 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//
using UnityEngine;
using System.Collections;

namespace FastShadowReceiver {
	public class UnityProjector : IProjector {
		private Projector m_projector;
		private Transform m_projectorTransform;
		private bool m_isScaleEnabled;

		public event System.Action updateTransform; // this event will be triggered before Shadow Receivers use the transform of the projector.
		public void InvokeUpdateTransform()
		{
			if (updateTransform != null) {
				updateTransform();
			}
		}
		public UnityProjector(Projector unityProjector)
		{
			m_projector = unityProjector;
			m_projectorTransform = unityProjector.transform;
			m_isScaleEnabled = unityProjector.GetComponent("ProjectorForLWRP.ProjectorForLWRP") != null;
		}
		public Projector projector {
			get { return m_projector; }
			set {
				m_projector = value;
				m_projectorTransform = value.transform;
			}
		}
		public Vector3 position {
			get { return m_projectorTransform.position; }
		}
		public Vector3 direction {
			get { return m_projectorTransform.forward; }
		}
		public Quaternion rotation {
			get { return m_projectorTransform.rotation; }
		}
		public Matrix4x4 uvProjectionMatrix
		{
			get {
				Matrix4x4 matProjection;
				if (m_projector.orthographic)
				{
					float x = m_projector.aspectRatio * m_projector.orthographicSize;
					float y = m_projector.orthographicSize;
					matProjection = Matrix4x4.Ortho(-x, x, -y, y, m_projector.nearClipPlane, m_projector.farClipPlane);
				}
				else
				{
					matProjection = Matrix4x4.Perspective(m_projector.fieldOfView, m_projector.aspectRatio, m_projector.nearClipPlane, m_projector.farClipPlane);
				}
				matProjection.m00 *= 0.5f;
				matProjection.m02 += 0.5f * matProjection.m32;
				matProjection.m03 += 0.5f * matProjection.m33;
				matProjection.m11 *= 0.5f;
				matProjection.m12 += 0.5f * matProjection.m32;
				matProjection.m13 += 0.5f * matProjection.m33;
				float zScale = 1.0f / (m_projector.farClipPlane - m_projector.nearClipPlane);
				matProjection.m22 = zScale;
				matProjection.m23 = -zScale * m_projector.nearClipPlane;
				if (m_isScaleEnabled)
				{
					matProjection = matProjection * m_projectorTransform.worldToLocalMatrix;
				}
				else
				{
					matProjection = matProjection * Matrix4x4.Rotate(Quaternion.Inverse(m_projectorTransform.rotation)) * Matrix4x4.Translate(-m_projectorTransform.position);

				}
				return matProjection;
			}
		}
		public bool isOrthographic {
			get { return m_projector.orthographic; }
		}
		public float nearClipPlane {
			get { return m_projector.nearClipPlane; }
		}
		public float farClipPlane {
			get { return m_projector.farClipPlane; }
		}
		public virtual void GetPlaneIntersection (Vector3[] vertices, Plane plane)
		{
			Vector3 o = m_projectorTransform.position;
			if (plane.GetDistanceToPoint(o) < 0.0f) {
				vertices[0] = vertices[1] = vertices[2] = vertices[3] = Vector3.zero;
				return;
			}
			if (m_projector.orthographic) {
				float xSize = m_projector.orthographicSize * m_projector.aspectRatio;
				float ySize = m_projector.orthographicSize;
				Vector3 x, y;
				if (m_isScaleEnabled)
				{
					x = m_projectorTransform.localToWorldMatrix.GetColumn(0);
					y = m_projectorTransform.localToWorldMatrix.GetColumn(1);
				}
				else
				{
					x = m_projectorTransform.right;
					y = m_projectorTransform.up;
				}
				x *= xSize;
				y *= ySize;
				Vector3 z = m_projectorTransform.forward;
				Vector3 v0 = o - x - y;
				Vector3 v1 = o - x + y;
				Vector3 v2 = o + x - y;
				Vector3 v3 = o + x + y;
				float invZdotN = 1.0f/Vector3.Dot(z, plane.normal);
				vertices[0] = v0 - (invZdotN * (plane.distance + Vector3.Dot(v0, plane.normal))) * z;
				vertices[1] = v1 - (invZdotN * (plane.distance + Vector3.Dot(v1, plane.normal))) * z;
				vertices[2] = v2 - (invZdotN * (plane.distance + Vector3.Dot(v2, plane.normal))) * z;
				vertices[3] = v3 - (invZdotN * (plane.distance + Vector3.Dot(v3, plane.normal))) * z;
			}
			else {
				float ySize = Mathf.Tan(0.5f*Mathf.Deg2Rad*m_projector.fieldOfView);
				float xSize = ySize * m_projector.aspectRatio;
				Vector3 x, y, z;
				if (m_isScaleEnabled)
				{
					x = m_projectorTransform.localToWorldMatrix.GetColumn(0);
					y = m_projectorTransform.localToWorldMatrix.GetColumn(1);
					z = m_projectorTransform.localToWorldMatrix.GetColumn(3);
				}
				else
				{
					x = m_projectorTransform.right;
					y = m_projectorTransform.up;
					z = m_projectorTransform.forward;
				}
				x *= xSize;
				y *= ySize;
				Vector3 v0 = z - x - y;
				Vector3 v1 = z - x + y;
				Vector3 v2 = z + x - y;
				Vector3 v3 = z + x + y;
				float d = Vector3.Dot(plane.normal, o) + plane.distance;
				vertices[0] = o - (d/Vector3.Dot(plane.normal, v0))*v0;
				vertices[1] = o - (d/Vector3.Dot(plane.normal, v1))*v1;
				vertices[2] = o - (d/Vector3.Dot(plane.normal, v2))*v2;
				vertices[3] = o - (d/Vector3.Dot(plane.normal, v3))*v3;
			}
		}
		public virtual void GetClipPlanes(ref ClipPlanes clipPlanes, Transform clipPlaneTransform)
		{
			Vector3 x, y, z, o;
			if (m_isScaleEnabled)
			{
				x = Vector3.right;
				y = Vector3.up;
				z = Vector3.forward;
				o = Vector3.zero;
			}
			else
			{
				x = m_projectorTransform.right;
				y = m_projectorTransform.up;
				z = m_projectorTransform.forward;
				o = m_projectorTransform.position;
			}
			if (m_projector.orthographic) {
				clipPlanes.SetClipPlaneNum(3, 2, true);
				float xSize = m_projector.orthographicSize * m_projector.aspectRatio;
				float ySize = m_projector.orthographicSize;
				clipPlanes.clipPlanes[0] = new Plane(x, o);
				clipPlanes.clipPlanes[0].distance += xSize;
				clipPlanes.maxDistance[0] = 2*xSize;
				clipPlanes.clipPlanes[1] = new Plane(y, o);
				clipPlanes.clipPlanes[1].distance += ySize;
				clipPlanes.maxDistance[1] = 2*ySize;
				clipPlanes.clipPlanes[2] = new Plane(z, o);
				clipPlanes.clipPlanes[2].distance -= m_projector.nearClipPlane;
				clipPlanes.maxDistance[2] = m_projector.farClipPlane - m_projector.nearClipPlane;
			}
			else {
				clipPlanes.SetClipPlaneNum(5, 4, false);
				float ySize = Mathf.Tan(0.5f*Mathf.Deg2Rad*m_projector.fieldOfView);
				float xSize = ySize * m_projector.aspectRatio;
				Vector3 x0 = (x + xSize*z).normalized;
				Vector3 x1 = (-x + xSize*z).normalized;
				Vector3 y0 = (y + ySize*z).normalized;
				Vector3 y1 = (-y + ySize*z).normalized;
				clipPlanes.clipPlanes[0] = new Plane(x0, o);
				clipPlanes.clipPlanes[1] = new Plane(y0, o);
				clipPlanes.clipPlanes[2] = new Plane(x1, o);
				clipPlanes.clipPlanes[3] = new Plane(y1, o);
				clipPlanes.clipPlanes[4] = new Plane(-z, o);
				clipPlanes.clipPlanes[4].distance += m_projector.farClipPlane;
				clipPlanes.maxDistance[0] = clipPlanes.maxDistance[2] = 2.0f * xSize * m_projector.farClipPlane;
				clipPlanes.maxDistance[1] = clipPlanes.maxDistance[3] = 2.0f * ySize * m_projector.farClipPlane;
				clipPlanes.maxDistance[4] = m_projector.farClipPlane - m_projector.nearClipPlane;
			}
			if (clipPlaneTransform != null || m_isScaleEnabled) {
				Matrix4x4 m;
				if (m_isScaleEnabled)
				{
					if (clipPlaneTransform != null)
					{
						m = (m_projectorTransform.worldToLocalMatrix * clipPlaneTransform.localToWorldMatrix).transpose;
					}
					else
					{
						m = m_projectorTransform.worldToLocalMatrix.transpose;
					}
				}
				else
				{
					m = clipPlaneTransform.localToWorldMatrix.transpose;
				}
				Vector3 t = m.GetRow(3);
				for (int i = 0; i < clipPlanes.clipPlaneCount; ++i) {
					float d = Vector3.Dot (clipPlanes.clipPlanes[i].normal, t);
					clipPlanes.clipPlanes[i].distance += d;
					clipPlanes.clipPlanes[i].normal = m.MultiplyVector(clipPlanes.clipPlanes[i].normal);
				}
			}
		}
		public virtual void GetClipPlanes(ref ClipPlanes clipPlanes, Transform clipPlaneTransform, ITransformPredictor predictor)
		{
			Vector3 x, y, z, o;
			Bounds angleBounds = predictor.PredictNextFrameEulerAngleChanges();
			Bounds moveBounds = predictor.PredictNextFramePositionChanges();
			if (m_isScaleEnabled)
			{
				x = Vector3.right;
				y = Vector3.up;
				z = Vector3.forward;
				o = moveBounds.center;
			}
			else
			{
				x = m_projectorTransform.right;
				y = m_projectorTransform.up;
				z = m_projectorTransform.forward;
				o = m_projectorTransform.position + m_projectorTransform.TransformDirection(moveBounds.center);
			}
			if (angleBounds.center != Vector3.zero) {
				Quaternion rot = Quaternion.Euler(angleBounds.center);
				if (!m_isScaleEnabled)
				{
					rot = m_projectorTransform.rotation * rot * Quaternion.Inverse(m_projectorTransform.rotation);
				}
				x = rot * x;
				y = rot * y;
				z = rot * z;
			}
			if (m_projector.orthographic) {
				clipPlanes.SetClipPlaneNum(3, 2, true);
				float farExtentX = Mathf.Tan(Mathf.Deg2Rad * Mathf.Min(80, angleBounds.extents.y)) * m_projector.farClipPlane;
				float farExtentY = Mathf.Tan(Mathf.Deg2Rad * Mathf.Min(80, angleBounds.extents.x)) * m_projector.farClipPlane;
				float xSize = m_projector.orthographicSize * m_projector.aspectRatio + farExtentX + moveBounds.extents.x;
				float ySize = m_projector.orthographicSize + farExtentY + moveBounds.extents.y;
				float cosZ = Mathf.Cos(Mathf.Deg2Rad * Mathf.Min(90, angleBounds.extents.z));
				float rcpLen = 1.0f/Mathf.Sqrt(xSize*xSize + ySize*ySize);
				float cosZ_x = Mathf.Max(cosZ, xSize*rcpLen);
				float cosZ_y = Mathf.Max(cosZ, ySize*rcpLen);
				float xSin = Mathf.Sqrt(1.0f - cosZ_y*cosZ_y) * xSize;
				float ySin = Mathf.Sqrt(1.0f - cosZ_x*cosZ_x) * ySize;
				xSize = cosZ_x * xSize + ySin;
				ySize = cosZ_y * ySize + xSin;
				clipPlanes.clipPlanes[0] = new Plane(x, o);
				clipPlanes.clipPlanes[0].distance += xSize;
				clipPlanes.maxDistance[0] = 2*xSize;
				clipPlanes.clipPlanes[1] = new Plane(y, o);
				clipPlanes.clipPlanes[1].distance += ySize;
				clipPlanes.maxDistance[1] = 2*ySize;
				clipPlanes.clipPlanes[2] = new Plane(z, o);
				clipPlanes.clipPlanes[2].distance -= m_projector.nearClipPlane - moveBounds.extents.z;
				clipPlanes.maxDistance[2] = m_projector.farClipPlane - m_projector.nearClipPlane + 2.0f*moveBounds.extents.z;
			}
			else {
				clipPlanes.SetClipPlaneNum(5, 4, false);
				float ySize = Mathf.Tan(Mathf.Deg2Rad*Mathf.Min(0.5f*m_projector.fieldOfView + angleBounds.extents.x, 80)) + moveBounds.extents.y;
				float xSize = Mathf.Tan(0.5f*Mathf.Deg2Rad*m_projector.fieldOfView) * m_projector.aspectRatio;
				float extentX = Mathf.Tan(Mathf.Deg2Rad * Mathf.Min (80, angleBounds.extents.y));
				xSize = (xSize + extentX)/Mathf.Max (0.1f, 1.0f - xSize * extentX) + moveBounds.extents.x;
				float cosZ = Mathf.Cos(Mathf.Deg2Rad * Mathf.Min(90, angleBounds.extents.z));
				float rcpLen = 1.0f/Mathf.Sqrt(xSize*xSize + ySize*ySize);
				float cosZ_x = Mathf.Max(cosZ, xSize*rcpLen);
				float cosZ_y = Mathf.Max(cosZ, ySize*rcpLen);
				float xSin = Mathf.Sqrt(1.0f - cosZ_y*cosZ_y) * xSize;
				float ySin = Mathf.Sqrt(1.0f - cosZ_x*cosZ_x) * ySize;
				xSize = cosZ_x * xSize + ySin;
				ySize = cosZ_y * ySize + xSin;
				o = o - moveBounds.extents.z * z;
				float far = m_projector.farClipPlane + 2.0f*moveBounds.extents.z;
				Vector3 x0 = (x + xSize*z).normalized;
				Vector3 x1 = (-x + xSize*z).normalized;
				Vector3 y0 = (y + ySize*z).normalized;
				Vector3 y1 = (-y + ySize*z).normalized;
				clipPlanes.clipPlanes[0] = new Plane(x0, o);
				clipPlanes.clipPlanes[1] = new Plane(y0, o);
				clipPlanes.clipPlanes[2] = new Plane(x1, o);
				clipPlanes.clipPlanes[3] = new Plane(y1, o);
				clipPlanes.clipPlanes[4] = new Plane(-z, o);
				clipPlanes.clipPlanes[4].distance += far;
				clipPlanes.maxDistance[0] = clipPlanes.maxDistance[2] = 2.0f * xSize * far;
				clipPlanes.maxDistance[1] = clipPlanes.maxDistance[3] = 2.0f * ySize * far;
				clipPlanes.maxDistance[4] = far - m_projector.nearClipPlane;
			}
			if (clipPlaneTransform != null || m_isScaleEnabled)
			{
				Matrix4x4 m;
				if (m_isScaleEnabled)
				{
					if (clipPlaneTransform != null)
					{
						m = (m_projectorTransform.worldToLocalMatrix * clipPlaneTransform.localToWorldMatrix).transpose;
					}
					else
					{
						m = m_projectorTransform.worldToLocalMatrix.transpose;
					}
				}
				else
				{
					m = clipPlaneTransform.localToWorldMatrix.transpose;
				}
				Vector3 t = m.GetRow(3);
				for (int i = 0; i < clipPlanes.clipPlaneCount; ++i)
				{
					float d = Vector3.Dot(clipPlanes.clipPlanes[i].normal, t);
					clipPlanes.clipPlanes[i].distance += d;
					clipPlanes.clipPlanes[i].normal = m.MultiplyVector(clipPlanes.clipPlanes[i].normal);
				}
			}
		}
	}
}
