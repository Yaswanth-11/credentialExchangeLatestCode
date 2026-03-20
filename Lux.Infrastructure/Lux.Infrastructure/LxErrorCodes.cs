using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lux.Infrastructure
{
    public static class LxErrorCodes
    {
        private static Dictionary<int, string> _messages = null;
        public const int E_UNSPECIFIED_ERROR = 1000;
        public const int E_INVALID_ARGUMENT = 1001;
        public const int E_INVALID_XML_FORMAT = 1002;
        public const int E_ERROR_GENERATING_TOKEN = 1003;
        public const int E_PKI_NATIVE_INIT = 1004;
        public const int E_PKI_NATIVE_ADD_CHECKSUM = 1005;
        public const int E_PKI_NATIVE_FREE_MEM = 1006;
        public const int E_PKI_NATIVE_GET_MSG = 1007;
        public const int E_GET_BY_ID_ASYNC = 1008;
        public const int E_GET_BY_ID = 1009;
        public const int E_GET_ALL_ASYNC = 1010;
        public const int E_ADD_ASYNC = 1011;
        public const int E_ADD = 1012;
        public const int E_UPDATE = 1013;
        public const int E_REMOVE = 1014;
        public const int E_RELOAD = 1015;
        public const int E_ADAPT_CLIENT_MODEL_TO_DATUM = 1016;
        public const int E_CLIENT_ENROLL_SERVICE = 1017;
        public const int E_ADAPT_CLIENT_DTO_TO_MODEL = 1018;
        public const int E_FIND_USER = 1019;
        public const int E_VALIDATE_USER = 1020;
        public const int E_VALIDATE_USER_SERVICE = 1021;
        public const int E_ADAPT_USER_DTO_TO_USERCRED = 1022;
        public const int E_ADAPT_RESET_USER_DTO_TO_USERCRED = 1023;
        public const int E_RESET_USER_SERVICE = 1024;
        public const int E_RESET_USER = 1025;
        public const int E_ADAPT_LICENSEDTO_TO_LICENSEDATUM = 1026;
        public const int E_ADAPT_LICENSEDTO_TO_APPIDDATUM = 1027;
        public const int E_GET_MATCHED_APPIDS = 1028;
        public const int E_DUPLICATE_APPID = 1029;
        public const int E_GENERATE_LICENSE_SERVICE = 1030;
        public const int E_GET_ALL_LICENSES_SERVICE = 1031;
        public const int E_GET_ALL = 1032;
        public const int E_GET_CLIENT_NAME = 1033;
        public const int E_ADAPT_CLIENT_DATUM_TO_MODEL = 1034;
        public const int E_GET_ALL_CLIENTS_SERVICE = 1035;
        public const int E_INVALID_EXPIRY_DATE = 1036;
        public const int E_UPDATE_LICENSE = 1037;
        public const int E_INVALID_LICENSE = 1038;
        public const int E_UPDATE_LICENSE_SERVICE = 1039;
        public const int E_INVALID_APP_ID = 1040;
        public const int E_GET_BY_APP_ID = 1041;
        public const int E_ADAPT_TRANSACTIONLOG_DTO_TO_TRANSACTIONLOG = 1042;
        public const int E_LICENSE_EXPIRED = 1043;
        public const int E_FIND_CUSTOMERID = 1044;
        public const int E_ADAPT_APPID_DATUM_TO_TRANSACTION_USAGE = 1045;
        public const int E_TRANSACTION_USAGE = 1046;
        public const int E_FETCH_CLIENT_NAME_BY_ID = 1047;
        public const int E_ADAPT_CONTRACTDETAIL_DTO_TO_CONTRACTDETAIL_MODEL = 1048;
        public const int E_ADAPT_CONTRACTPRICING_DTO_TO_CONTRACTPRICING_MODEL = 1049;
        public const int E_ADAPT_CONTRACTDETAIL_MODEL_TO_CONTRACTDETAIL = 1050;
        public const int E_ADAPT_CONTRACTPRICING_MODEL_TO_CONTRACTPRICING = 1051;
        public const int E_CONTRACT_DETAIL_ENROLL_SERVICE = 1052;
        public const int E_CONTRACT_PRICING_ENROLL_SERVICE = 1053;
        public const int E_GET_LICENSE_SERVICE = 1054;
        public const int E_GET_LICENSE_BY_ID = 1055;
        public const int E_GET_TRANSACTION_USAGE_SERVICE = 1056;
        public const int E_GET_LICENSE_BY_ID_SERVICE = 1057;
        public const int E_ADAPT_TRANSACTION_USAGE_TO_MODEL = 1058;
        public const int E_ADAPT_LICENSE_DATUM_TO_MODEL = 1059;
        public const int E_GET_TRANSACTION_LOGS = 1060; 
        public const int E_ADAPT_TRANSACTION_LOG_TO_MODEL = 1061; 
        public const int E_GET_TRANSACTION_LOG_SERVICE = 1062; 
        public const int E_CREATE_JWT_TOKEN = 1063; 
        public const int E_GET_VALID_CLIENTS_SERVICE = 1064; 
        public const int E_GET_APPID_BY_CLIENT = 1065;
        public const int E_GET_APPID_BY_CLIENT_SERVICE = 1066;
        public const int E_ADAPT_UPDATE_APPID_DTO_TO_DATUM = 1067;
        public const int E_UPDATE_APPID_SERVICE = 1068;
        public const int E_TOTAL_COUNT_SERVICE = 1069;
        public const int E_GET_CONTRACT_DATE = 1070;
        public const int E_GET_CLIENT_CONTRACTS = 1071;
        public const int E_GET_CLIENT_CONTRACT_DETAILS_SERVICE = 1072;
        public const int E_CREATE_EMAIL_MESSAGE = 1073;
        public const int E_SEND_MAIL = 1074;
        public const int E_SEND_MAIL_SERVICE = 1075;
        public const int E_GET_KYB_DATA = 1076;
        public const int E_CLIENT_UPDATE = 1077;
        public const int E_LICENSE_CONTROL = 1078;
        public const int E_ADAPT_TO_LICENSE_CONTROL = 1079;
        public const int E_ADAPT_CLIENT_UPDATEDTO_TO_CLIENT_ENROLL_MODEL = 1080;
        public const int E_LOG_TRANSACTION = 1081;
        public const int E_INVALID_TSA_DATA = 1082;
        public const int E_USER_EXIST = 1083;
        public const int E_USER_NOT_EXIST = 1084;
        public const int E_USER_ENROLL = 1085;
        public const int E_FIND_USER_MOBILE = 1086;
        public const int E_PKI_GET_EC_KEY = 1087;
        public const int E_PKI_NATIVE_DECRYPT_ERROR = 1088;
        public const int E_PKI_PREPARE_MDOC_REQUEST = 1089;
        public const int E_PKI_PARSE_MDOC_DEVICE_ENGAGEMENT_DATA = 1090;
        public const int E_NOT_FOUND = 1091;
        public const int  E_SUCCESS = 1092;
        public const int E_INTERNAL_ERROR = 1093;
        public const int E_INVALID_REQUEST = 1094;
        private static Dictionary<int, string> Messages
        {
            get
            {
                if (null == _messages)
                {
                    _messages = new Dictionary<int, string>();
                    _messages.Add(E_UNSPECIFIED_ERROR, "Unspecified error");
                    _messages.Add(E_INVALID_ARGUMENT, "Invalid parameter");
                    _messages.Add(E_INVALID_XML_FORMAT, "Invalid xml data");
                    _messages.Add(E_ERROR_GENERATING_TOKEN, "Error occurred in generating token");
                    _messages.Add(E_PKI_NATIVE_INIT, "Error occurred in PKI native initialization method ");
                    _messages.Add(E_PKI_NATIVE_ADD_CHECKSUM, "Error occurred in PKI native add checksum method");
                    _messages.Add(E_PKI_NATIVE_FREE_MEM, "Error occurred in PKI native add free memory method");
                    _messages.Add(E_PKI_NATIVE_GET_MSG, "Error occurred in PKI native get message method");
                    _messages.Add(E_GET_BY_ID_ASYNC, "Error occurred in Repository get by ID async method");
                    _messages.Add(E_GET_BY_ID, "Error occurred in Repository get by ID async method");
                    _messages.Add(E_GET_ALL_ASYNC, "Error occurred in Repository get all async method");
                    _messages.Add(E_ADD_ASYNC, "Error occurred in Repository add async method");
                    _messages.Add(E_ADD, "Error occurred in Repository add method");
                    _messages.Add(E_UPDATE, "Error occurred in Repository udpate method");
                    _messages.Add(E_REMOVE, "Error occurred in Repository remove method");
                    _messages.Add(E_RELOAD, "Error occurred in Repository reload method");
                    _messages.Add(E_ADAPT_CLIENT_MODEL_TO_DATUM, "Error occurred in converting client model to datum");
                    _messages.Add(E_CLIENT_ENROLL_SERVICE, "Error occurred in client enroll service");
                    _messages.Add(E_ADAPT_CLIENT_DTO_TO_MODEL, "Error occurred in converting client dto to model");
                    _messages.Add(E_FIND_USER, "Could not find user using the username and password");
                    _messages.Add(E_VALIDATE_USER, "Could not validate user using the username and password");
                    _messages.Add(E_VALIDATE_USER_SERVICE, "Error occurred in validate user service");
                    _messages.Add(E_ADAPT_USER_DTO_TO_USERCRED, "Error occurred in converting usercredentialDTO to usercredential");
                    _messages.Add(E_ADAPT_RESET_USER_DTO_TO_USERCRED, "Error occurred in converting resetusercredentialDTO to usercredential");
                    _messages.Add(E_RESET_USER_SERVICE, "Error occurred in reset user password service");
                    _messages.Add(E_RESET_USER, "Error occurred in resetting user password");
                    _messages.Add(E_ADAPT_LICENSEDTO_TO_LICENSEDATUM, "Error occurred in converting licenseDTO to licenseDatum");
                    _messages.Add(E_ADAPT_LICENSE_DATUM_TO_MODEL, "Error occurred in converting licenseDatum TO license data model");
                    _messages.Add(E_ADAPT_LICENSEDTO_TO_APPIDDATUM, "Error occurred in converting licenseDTO to appIdDatum");
                    _messages.Add(E_GET_MATCHED_APPIDS, "Error occurred in retrieving app id data");
                    _messages.Add(E_DUPLICATE_APPID, "Error occurred in GetMatchedAppIds due to duplicate app id");
                    _messages.Add(E_GENERATE_LICENSE_SERVICE, "Error occurred in generate license service");
                    _messages.Add(E_GET_ALL_LICENSES_SERVICE, "Error occurred in get all licenses service");
                    _messages.Add(E_GET_ALL, "Error occurred in Repository get all method");
                    _messages.Add(E_GET_CLIENT_NAME, "Error occurred in Repository get client name method");
                    _messages.Add(E_ADAPT_CLIENT_DATUM_TO_MODEL, "Error occurred in converting client datum to model");
                    _messages.Add(E_GET_ALL_CLIENTS_SERVICE, "Error occurred in get all clients service");
                    _messages.Add(E_INVALID_EXPIRY_DATE, "Invalid expiry date");
                    _messages.Add(E_INVALID_LICENSE, "Invalid license key");
                    _messages.Add(E_UPDATE_LICENSE, "Error occurred in updating license");
                    _messages.Add(E_UPDATE_LICENSE_SERVICE, "Error occurred in update license service");
                    _messages.Add(E_INVALID_APP_ID, "Invalid app id");
                    _messages.Add(E_GET_BY_APP_ID, "Error occured in repository get by app id");
                    _messages.Add(E_ADAPT_TRANSACTIONLOG_DTO_TO_TRANSACTIONLOG, "Error occured in converting transactionlogDTO to transactionlog");
                    _messages.Add(E_LICENSE_EXPIRED, "Error occured in transactionlog service due to license expiry");
                    _messages.Add(E_FIND_CUSTOMERID, "Could not find the customerId ");
                    _messages.Add(E_ADAPT_APPID_DATUM_TO_TRANSACTION_USAGE, "Error occurred in converting appid datum to transaction usage ");
                    _messages.Add(E_TRANSACTION_USAGE, "Error occurred in transaction usage repository");
                    _messages.Add(E_FETCH_CLIENT_NAME_BY_ID, "Could not fetch client name using ID");
                    _messages.Add(E_ADAPT_CONTRACTDETAIL_DTO_TO_CONTRACTDETAIL_MODEL, "Error occurred in converting contract detail DTO to contract detail model");
                    _messages.Add(E_ADAPT_CONTRACTPRICING_DTO_TO_CONTRACTPRICING_MODEL, "Error occurred in converting contract pricing DTO to contract pricing model");
                    _messages.Add(E_ADAPT_CONTRACTDETAIL_MODEL_TO_CONTRACTDETAIL, "Error occurred in converting contract detail model to contract detail");
                    _messages.Add(E_ADAPT_CONTRACTPRICING_MODEL_TO_CONTRACTPRICING, "Error occurred in converting contract pricing model to contract pricing");
                    _messages.Add(E_ADAPT_UPDATE_APPID_DTO_TO_DATUM, "Error occurred in converting update app id DTO to datum");
                    _messages.Add(E_ADAPT_TRANSACTION_LOG_TO_MODEL, "Error occurred in converting transaction log to model");
                    _messages.Add(E_ADAPT_TRANSACTION_USAGE_TO_MODEL, "Error occurred in converting transaction usage to model");
                    _messages.Add(E_CONTRACT_DETAIL_ENROLL_SERVICE, "Error occurred in contract detail enroll service");
                    _messages.Add(E_CONTRACT_PRICING_ENROLL_SERVICE, "Error occurred in contract pricing enroll service");
                    _messages.Add(E_GET_LICENSE_SERVICE, "Error occurred in fetching license data");
                    _messages.Add(E_GET_TRANSACTION_USAGE_SERVICE, "Error occurred in fetching transaction usage data");
                    _messages.Add(E_GET_LICENSE_BY_ID, "Error occurred in repository get license by id");
                    _messages.Add(E_GET_LICENSE_BY_ID_SERVICE, "Error occurred in get license by id service");
                    _messages.Add(E_GET_APPID_BY_CLIENT_SERVICE, "Error occurred in get valid clients service");
                    _messages.Add(E_GET_TRANSACTION_LOG_SERVICE, "Error occurred in get transaction logs service");
                    _messages.Add(E_UPDATE_APPID_SERVICE, "Error occurred in update app id service");
                    _messages.Add(E_TOTAL_COUNT_SERVICE, "Error occurred in total count service");
                    _messages.Add(E_SEND_MAIL_SERVICE, "Error occurred in send mail service");
                    _messages.Add(E_GET_CLIENT_CONTRACT_DETAILS_SERVICE, "Error occurred in get client contract details service");
                    _messages.Add(E_GET_TRANSACTION_LOGS, "Error occurred in transaction logs repository");
                    _messages.Add(E_CREATE_JWT_TOKEN, "Error occurred in creating jwt token");
                    _messages.Add(E_GET_APPID_BY_CLIENT, "Error occurred in repository get app id by client");
                    _messages.Add(E_GET_CONTRACT_DATE, "Error occurred in repository get contract date");
                    _messages.Add(E_GET_CLIENT_CONTRACTS, "Error occurred in repository get client contracts");
                    _messages.Add(E_CREATE_EMAIL_MESSAGE, "Error occurred in creating email message");
                    _messages.Add(E_SEND_MAIL, "Error occurred in sending mail");
                    _messages.Add(E_GET_KYB_DATA, "Error occurred in repository for getting kyb data ");
                    _messages.Add(E_CLIENT_UPDATE, "Error occurred in repository for updating kyb data ");
                    _messages.Add(E_LICENSE_CONTROL, "Error occurred in repository license control ");
                    _messages.Add(E_ADAPT_TO_LICENSE_CONTROL, "Error occurred in converting to license control data ");
                    _messages.Add(E_ADAPT_CLIENT_UPDATEDTO_TO_CLIENT_ENROLL_MODEL, "Error occurred in converting client update DTO to client enroll model");
                    _messages.Add(E_LOG_TRANSACTION, "Error occurred in transaction log service ");
                    _messages.Add(E_INVALID_TSA_DATA, "Error occurred in retrieving TSA Data");
                    _messages.Add(E_USER_EXIST, "User already enrolled");
                    _messages.Add(E_USER_NOT_EXIST, "User not enrolled");
                    _messages.Add(E_USER_ENROLL, "Error occurred in enrolling user");
                    _messages.Add(E_FIND_USER_MOBILE, "Error occurred in fetching selfie by mobile number");
                    _messages.Add(E_PKI_GET_EC_KEY, "Error occurred in pki get EC key method");
                }
                return _messages;
            }
        }
        public static string GetErrorMessage(int code)
        {
            string errorMessage = null;
            if (Messages.TryGetValue(code, out errorMessage))
            {
                return errorMessage;
            }

            return "Unknown exception occurred";
        }
    }
}
