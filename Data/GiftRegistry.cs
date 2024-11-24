using Microsoft.AspNetCore.Mvc;
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
        Task<List<FamilyMember>> GetFamilyMembersAsync(string excludeEmail);
        Task<List<FamilyMember>> GetFamilyMembersAsync(Family targetFamily);
        Task<Family> CreateFamilyAsync(Family family);
        Task<Family> GetFamilyAsync(string familyId);
        Task<FamilyMember> GetMemberByUserIdAsync(string userId);
        Task<bool> CreateMemberAsync(FamilyMember member);
        Task<bool> AddMemberToFamily(string userId, Family family);
        Task<bool> CreateGiftRequestAsync(GiftRequest request);
        Task<GiftRequest> GetRequestAsync(string giftID, string email);
        Task<bool> UpdateRequestAsync(GiftRequest req);
        List<GiftRequest> GetMemberRequests(string email, bool self = true);

        Task<List<GiftRequest>> GetReservedGiftsAsync(string giverEmail);
        Task<Dictionary<string, string>> GetMemberMap(string familyId);
        void DeleteGiftRequest(string giftID, string memberEmail);
        Task<bool> HasFamily(string userId);
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
        public async Task<List<FamilyMember>> GetFamilyMembersAsync(string excludeEmail)
        {
            FamilyMember fm = await GetMemberByUserIdAsync(excludeEmail);

            if (fm == null)
            {
                throw new ApplicationException("User does not exist");
            }

            var members =
                from member
                    in _docClient.CreateDocumentQuery<FamilyMember>(_membersUri, new FeedOptions { EnableCrossPartitionQuery = true })
                        where member.FamilyID == fm.FamilyID && member.Email.ToLower() != excludeEmail.ToLower()
                            select member;

            return members.ToList();
        }

        public async Task<List<FamilyMember>> GetFamilyMembersAsync(Family targetFamily)
        {            
            var members =
                from member
                    in _docClient.CreateDocumentQuery<FamilyMember>(_membersUri, new FeedOptions { EnableCrossPartitionQuery = true })
                where member.FamilyID == targetFamily.FamilyID
                select member;

            return members.ToList();
        }

        public async Task<FamilyMember> GetMemberByUserIdAsync(string userId)
        {
            FeedIterator<FamilyMember> feed = _members.GetItemQueryIterator<FamilyMember>(
               queryText: $"SELECT * from members m WHERE m.id = '{userId}'"
           );

            while (feed.HasMoreResults)
            {
                Microsoft.Azure.Cosmos.FeedResponse<FamilyMember> r = await feed.ReadNextAsync();
                foreach (FamilyMember m in r.Resource)
                {
                    return m;
                }
            }
            return null;
        }

        public async Task<Family> CreateFamilyAsync(Family family)
        {
            family.Id = family.FamilyID;
            ItemResponse<Family> newFamily = await _families.CreateItemAsync<Family>(family);

            return newFamily.Resource;
        }

        public async Task<Family> GetFamilyAsync(string familyID)
        {
            ItemResponse<Family> resp = await _families.ReadItemAsync<Family>(familyID, new PartitionKey(familyID));

            return resp.Resource;
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

        public async Task<bool> AddMemberToFamily(string email, Family family)
        {
            FamilyMember fm = await GetMemberAsync(email, "");

            if (fm != null)
            { 
                await _members.DeleteItemAsync<FamilyMember>(fm.Id, new PartitionKey(""));

                fm.FamilyID = family.FamilyID;
                await CreateMemberAsync(fm);

                return true;
            }

            return false;
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
            await _requests.ReplaceItemAsync<GiftRequest>(req, req.Id, new PartitionKey(req.Id));

            return true;
        }

        public async Task<List<GiftRequest>> GetReservedGiftsAsync(string giverEmail)
        {
            var requests =
                from request
                    in _docClient.CreateDocumentQuery<GiftRequest>(_requestsUri, new FeedOptions { EnableCrossPartitionQuery = true })
                where request.GiverId.ToLower() == giverEmail.ToLower()
                    select request;

            return requests.ToList();
        }

        public async Task<Dictionary<string, string>> GetMemberMap(string familyId)
        {
            var result = new Dictionary<string, string>();

            Family f = await GetFamilyAsync(familyId);
            var members = await GetFamilyMembersAsync(f);

            foreach (FamilyMember mem in members)
            {
                result.Add(mem.Email, mem.FirstName);
            }

            return result;
        }

        public void DeleteGiftRequest(string giftID, string memberEmail)
        {
            _requests.DeleteItemAsync<GiftRequest>(giftID, new PartitionKey(giftID));
        }

        public async Task<bool> HasFamily(string userId)
        {
            bool result = false;

            FeedIterator<FamilyMember> feed = _members.GetItemQueryIterator<FamilyMember>(
                queryText: $"SELECT * from members m WHERE m.id = '{userId}'"
            );

            while (feed.HasMoreResults)
            {
                Microsoft.Azure.Cosmos.FeedResponse<FamilyMember> r = await feed.ReadNextAsync();
                foreach (FamilyMember m in r.Resource)
                {
                    if (!string.IsNullOrEmpty(m.FamilyID))
                    {
                        result = true; break;
                    }
                }
            }
            return result;
        }
    }
}