using System.Runtime.InteropServices;

namespace Steamworks
{
	[StructLayout(0, Pack = 4)]
	[CallbackIdentity(1326)]
	public struct RemoteStorageEnumerateUserSharedWorkshopFilesResult_t
	{
		public const int k_iCallback = 1326;

		public EResult m_eResult;

		public int m_nResultsReturned;

		public int m_nTotalResultCount;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
		public PublishedFileId_t[] m_rgPublishedFileId;
	}
}
