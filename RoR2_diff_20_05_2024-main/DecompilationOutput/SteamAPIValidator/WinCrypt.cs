using System;
using System.Runtime.InteropServices;

namespace SteamAPIValidator;

internal static class WinCrypt
{
	public struct BLOB
	{
		public int cbData;

		public IntPtr pbData;
	}

	public struct CRYPT_ALGORITHM_IDENTIFIER
	{
		public string pszObjId;

		private BLOB Parameters;
	}

	public struct CERT_ID
	{
		public int dwIdChoice;

		public BLOB IssuerSerialNumberOrKeyIdOrHashId;
	}

	public struct SIGNER_SUBJECT_INFO
	{
		public uint cbSize;

		public IntPtr pdwIndex;

		public uint dwSubjectChoice;

		public SubjectChoiceUnion Union1;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SubjectChoiceUnion
	{
		[FieldOffset(0)]
		public IntPtr pSignerFileInfo;

		[FieldOffset(0)]
		public IntPtr pSignerBlobInfo;
	}

	public struct CERT_NAME_BLOB
	{
		public uint cbData;

		[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
		public byte[] pbData;
	}

	public struct CRYPT_INTEGER_BLOB
	{
		public uint cbData;

		public IntPtr pbData;
	}

	public struct CRYPT_ATTR_BLOB
	{
		public uint cbData;

		[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
		public byte[] pbData;
	}

	public struct CRYPT_ATTRIBUTE
	{
		[MarshalAs(UnmanagedType.LPStr)]
		public string pszObjId;

		public uint cValue;

		[MarshalAs(UnmanagedType.LPStruct)]
		public CRYPT_ATTR_BLOB rgValue;
	}

	public struct CMSG_SIGNER_INFO
	{
		public int dwVersion;

		private CERT_NAME_BLOB Issuer;

		private CRYPT_INTEGER_BLOB SerialNumber;

		private CRYPT_ALGORITHM_IDENTIFIER HashAlgorithm;

		private CRYPT_ALGORITHM_IDENTIFIER HashEncryptionAlgorithm;

		private BLOB EncryptedHash;

		private CRYPT_ATTRIBUTE[] AuthAttrs;

		private CRYPT_ATTRIBUTE[] UnauthAttrs;
	}

	public const int CRYPT_ASN_ENCODING = 1;

	public const int CRYPT_NDR_ENCODING = 2;

	public const int X509_ASN_ENCODING = 1;

	public const int X509_NDR_ENCODING = 2;

	public const int PKCS_7_ASN_ENCODING = 65536;

	public const int PKCS_7_NDR_ENCODING = 131072;

	public static UIntPtr PKCS7_SIGNER_INFO = new UIntPtr(500u);

	public static UIntPtr CMS_SIGNER_INFO = new UIntPtr(501u);

	public static string szOID_RSA_signingTime = "1.2.840.113549.1.9.5";

	public static string szOID_RSA_counterSign = "1.2.840.113549.1.9.6";

	public const int CMSG_TYPE_PARAM = 1;

	public const int CMSG_CONTENT_PARAM = 2;

	public const int CMSG_BARE_CONTENT_PARAM = 3;

	public const int CMSG_INNER_CONTENT_TYPE_PARAM = 4;

	public const int CMSG_SIGNER_COUNT_PARAM = 5;

	public const int CMSG_SIGNER_INFO_PARAM = 6;

	public const int CMSG_SIGNER_CERT_INFO_PARAM = 7;

	public const int CMSG_SIGNER_HASH_ALGORITHM_PARAM = 8;

	public const int CMSG_SIGNER_AUTH_ATTR_PARAM = 9;

	public const int CMSG_SIGNER_UNAUTH_ATTR_PARAM = 10;

	public const int CMSG_CERT_COUNT_PARAM = 11;

	public const int CMSG_CERT_PARAM = 12;

	public const int CMSG_CRL_COUNT_PARAM = 13;

	public const int CMSG_CRL_PARAM = 14;

	public const int CMSG_ENVELOPE_ALGORITHM_PARAM = 15;

	public const int CMSG_RECIPIENT_COUNT_PARAM = 17;

	public const int CMSG_RECIPIENT_INDEX_PARAM = 18;

	public const int CMSG_RECIPIENT_INFO_PARAM = 19;

	public const int CMSG_HASH_ALGORITHM_PARAM = 20;

	public const int CMSG_HASH_DATA_PARAM = 21;

	public const int CMSG_COMPUTED_HASH_PARAM = 22;

	public const int CMSG_ENCRYPT_PARAM = 26;

	public const int CMSG_ENCRYPTED_DIGEST = 27;

	public const int CMSG_ENCODED_SIGNER = 28;

	public const int CMSG_ENCODED_MESSAGE = 29;

	public const int CMSG_VERSION_PARAM = 30;

	public const int CMSG_ATTR_CERT_COUNT_PARAM = 31;

	public const int CMSG_ATTR_CERT_PARAM = 32;

	public const int CMSG_CMS_RECIPIENT_COUNT_PARAM = 33;

	public const int CMSG_CMS_RECIPIENT_INDEX_PARAM = 34;

	public const int CMSG_CMS_RECIPIENT_ENCRYPTED_KEY_INDEX_PARAM = 35;

	public const int CMSG_CMS_RECIPIENT_INFO_PARAM = 36;

	public const int CMSG_UNPROTECTED_ATTR_PARAM = 37;

	public const int CMSG_SIGNER_CERT_ID_PARAM = 38;

	public const int CMSG_CMS_SIGNER_INFO_PARAM = 39;

	public const int CERT_QUERY_OBJECT_FILE = 1;

	public const int CERT_QUERY_OBJECT_BLOB = 2;

	public const int CERT_QUERY_CONTENT_CERT = 1;

	public const int CERT_QUERY_CONTENT_CTL = 2;

	public const int CERT_QUERY_CONTENT_CRL = 3;

	public const int CERT_QUERY_CONTENT_SERIALIZED_STORE = 4;

	public const int CERT_QUERY_CONTENT_SERIALIZED_CERT = 5;

	public const int CERT_QUERY_CONTENT_SERIALIZED_CTL = 6;

	public const int CERT_QUERY_CONTENT_SERIALIZED_CRL = 7;

	public const int CERT_QUERY_CONTENT_PKCS7_SIGNED = 8;

	public const int CERT_QUERY_CONTENT_PKCS7_UNSIGNED = 9;

	public const int CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED = 10;

	public const int CERT_QUERY_CONTENT_PKCS10 = 11;

	public const int CERT_QUERY_CONTENT_PFX = 12;

	public const int CERT_QUERY_CONTENT_CERT_PAIR = 13;

	public const int CERT_QUERY_CONTENT_FLAG_CERT = 2;

	public const int CERT_QUERY_CONTENT_FLAG_CTL = 4;

	public const int CERT_QUERY_CONTENT_FLAG_CRL = 8;

	public const int CERT_QUERY_CONTENT_FLAG_SERIALIZED_STORE = 16;

	public const int CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT = 32;

	public const int CERT_QUERY_CONTENT_FLAG_SERIALIZED_CTL = 64;

	public const int CERT_QUERY_CONTENT_FLAG_SERIALIZED_CRL = 128;

	public const int CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED = 256;

	public const int CERT_QUERY_CONTENT_FLAG_PKCS7_UNSIGNED = 512;

	public const int CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED = 1024;

	public const int CERT_QUERY_CONTENT_FLAG_PKCS10 = 2048;

	public const int CERT_QUERY_CONTENT_FLAG_PFX = 4096;

	public const int CERT_QUERY_CONTENT_FLAG_CERT_PAIR = 8192;

	public const int CERT_QUERY_CONTENT_FLAG_ALL = 16382;

	public const int CERT_QUERY_FORMAT_BINARY = 1;

	public const int CERT_QUERY_FORMAT_BASE64_ENCODED = 2;

	public const int CERT_QUERY_FORMAT_ASN_ASCII_HEX_ENCODED = 3;

	public const int CERT_QUERY_FORMAT_FLAG_BINARY = 2;

	public const int CERT_QUERY_FORMAT_FLAG_BASE64_ENCODED = 4;

	public const int CERT_QUERY_FORMAT_FLAG_ASN_ASCII_HEX_ENCODED = 8;

	public const int CERT_QUERY_FORMAT_FLAG_ALL = 14;

	[DllImport("crypt32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern bool CryptQueryObject(int dwObjectType, IntPtr pvObject, int dwExpectedContentTypeFlags, int dwExpectedFormatTypeFlags, int dwFlags, out int pdwMsgAndCertEncodingType, out int pdwContentType, out int pdwFormatType, ref IntPtr phCertStore, ref IntPtr phMsg, ref IntPtr ppvContext);

	[DllImport("crypt32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern bool CryptMsgGetParam(IntPtr hCryptMsg, int dwParamType, int dwIndex, IntPtr pvData, ref int pcbData);

	[DllImport("crypt32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern bool CryptMsgGetParam(IntPtr hCryptMsg, int dwParamType, int dwIndex, [In][Out] byte[] vData, ref int pcbData);

	[DllImport("crypt32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool CryptDecodeObject(uint CertEncodingType, UIntPtr lpszStructType, byte[] pbEncoded, uint cbEncoded, uint flags, [In][Out] byte[] pvStructInfo, ref uint cbStructInfo);
}
