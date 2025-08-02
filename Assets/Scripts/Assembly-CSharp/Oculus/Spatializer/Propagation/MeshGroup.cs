using System;
using System.Runtime.InteropServices;

namespace Oculus.Spatializer.Propagation
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct MeshGroup
	{
		public UIntPtr indexOffset;

		public UIntPtr faceCount;

		[MarshalAs(UnmanagedType.U4)]
		public FaceType faceType;

		public IntPtr material;
	}
}
