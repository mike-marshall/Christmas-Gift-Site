using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PolarExpress3.Data
{
    public interface IGiftRegistry
    {
        Task<FamilyMember> GetMemberAsync(string email, string familyID = "togo");
        public List<FamilyMember> GetFamilyMembersAsync(string excludeEmail);
        Task<bool> CreateFamilyAsync(Family family);
        Task<bool> CreateMemberAsync(FamilyMember member);
        Task<bool> CreateGiftRequestAsync(GiftRequest request);
        Task<GiftRequest> GetRequestAsync(string giftID, string email);
        Task<bool> UpdateRequestAsync(GiftRequest req);
        List<GiftRequest> GetMemberRequests(string email, bool self = true);

        List<GiftRequest> GetReservedGifts(string giverEmail);
        Dictionary<string, string> GetMemberMap();
        void DeleteGiftRequest(string giftID, string memberEmail);
    }
    public class GiftRegistry : IGiftRegistry
    {
        IConfiguration _config;
        string _endPointURI;
        string _cosmosKey;
        CosmosClient _client;
        DocumentClient _docClient;
        Database _db;
        Container _families;
        Container _members;
        Container _requests;
        Uri _membersUri, _requestsUri;

        public GiftRegistry(IConfiguration config)
        {
            _config = config;
            _endPointURI = config.GetValue<string>("Cosmos:EndpointUri");
            _cosmosKey = config.GetValue<string>("Cosmos:PrimaryKey");
            _client = new CosmosClient(_endPointURI, _cosmosKey, 
                    new CosmosClientOptions() { ApplicationName = "PolarExpressApp" }
            );
            _docClient = new DocumentClient(new Uri(_endPointURI), _cosmosKey,
                    new ConnectionPolicy { ConnectionMode = Microsoft.Azure.Documents.Client.ConnectionMode.Gateway, ConnectionProtocol = Protocol.Https });

            _db = _client.GetDatabase("PolarExpress");
            _families = _db.GetContainer("Families");
            _members = _db.GetContainer("Members");
            _requests = _db.GetContainer("Requests");
            _membersUri = UriFactory.CreateDocumentCollectionUri("PolarExpress", "Members");
            _requestsUri = UriFactory.CreateDocumentCollectionUri("PolarExpress", "Requests");
        }
        public List<FamilyMember> GetFamilyMembersAsync(string excludeEmail)
        {
            var members =
                from member
                    in _docClient.CreateDocumentQuery<FamilyMember>(_membersUri, new FeedOptions { EnableCrossPartitionQuery = true })
                        where member.FamilyID == "togo" && member.Email.ToLower() != excludeEmail.ToLower()
                            select member;

            return members.ToList();
        }

        public async Task<bool> CreateFamilyAsync(Family family)
        {
            ItemResponse<Family> newFamily = await _families.CreateItemAsync<Family>(family);

            return true;
        }

        public async Task<bool> CreateMemberAsync(FamilyMember member)
        {
            ItemResponse<FamilyMember> newMember = await _members.CreateItemAsync<FamilyMember>(member);

            return true;
        }

        public async Task<bool> CreateGiftRequestAsync(GiftRequest request)
        {
            ItemResponse<GiftRequest> newGift = await _requests.CreateItemAsync<GiftRequest>(request);

            return true;
        }

        public async Task<FamilyMember> GetMemberAsync(string email, string familyID ="togo")
        {
            ItemResponse<FamilyMember> memberResp = await _members.ReadItemAsync<FamilyMember>(email, new PartitionKey(familyID));

            return memberResp.Resource;
        }

        public List<GiftRequest> GetMemberRequests(string email, bool self = true)
        {
            var requests =
                from request
                    in _docClient.CreateDocumentQuery<GiftRequest>(_requestsUri, new FeedOptions { EnableCrossPartitionQuery = true })
                        where request.MemberID == email && (self || null == request.GiverId)
                            select request;

            return requests.ToList();
        }

        public async Task<GiftRequest> GetRequestAsync(string giftID, string email)
        {

            ItemResponse<GiftRequest> memberResp = null;

            try
            {
                memberResp = await _requests.ReadItemAsync<GiftRequest>(giftID, new PartitionKey(giftID));
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Failed to get Request: {ex.Message}");
            }

            return memberResp.Resource;
        }

        public async Task<bool> UpdateRequestAsync(GiftRequest req)
        {
            await _requests.ReplaceItemAsync<GiftRequest>(req, req.Id, new PartitionKey(req.MemberID));

            return true;
        }

        public List<GiftRequest> GetReservedGifts(string giverEmail)
        {
            var requests =
                from request
                    in _docClient.CreateDocumentQuery<GiftRequest>(_requestsUri, new FeedOptions { EnableCrossPartitionQuery = true })
                where request.GiverId.ToLower() == giverEmail.ToLower()
                    select request;

            return requests.ToList();
        }

        public Dictionary<string, string> GetMemberMap()
        {
            var result = new Dictionary<string, string>();

            var members = GetFamilyMembersAsync(String.Empty);

            foreach (FamilyMember mem in members)
            {
                result.Add(mem.Email, mem.FirstName);
            }

            return result;
        }

        public void DeleteGiftRequest(string giftID, string memberEmail)
        {
            _requests.DeleteItemAsync<GiftRequest>(giftID, new PartitionKey(memberEmail));
        }
    }
}