# Redis Demo (.NET) - Steeltoe on TAS/TPCF

A .NET 8/10 / Steeltoe companion to the Java Spring Boot version — same endpoints, same Redis data structures, same Redis service binding. 
Validates connectivity and functional parity across both runtimes before migrating from Redis Enterprise to Credhub Service.
For the example steps below, it is assumed you have the CF cli and jq installed on your machine, and that you are logged into 
a TPCF foundation via the CF cli.

A Java (Spring Boot) version of this application is available at https://github.com/dawu415/redis-demo-java.

## Tech Stack

- **.NET 10**
- **Steeltoe 3.2** Redis Connector (auto-parses `VCAP_SERVICES`)
- **StackExchange.Redis**

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/items` | Create a new item |
| `GET` | `/api/items/{id}` | Get item by ID |
| `GET` | `/api/items` | Get all items |
| `GET` | `/api/items/search?name=X` | Search by name |
| `GET` | `/api/items/create?id=1&name=X&desc=Y` | Lazy create via GET |
| `PUT` | `/api/items/{id}` | Update an item |
| `DELETE` | `/api/items/{id}` | Delete an item |
| `GET` | `/api/items/info` | Connection diagnostics |

## Data Compatibility

Both the Java and .NET apps use the same Redis key patterns:
- Individual items: `items:{id}` (Redis hashes)
- ID tracking set: `items`

This means both apps can read/write the **same data** in the same Redis instance — useful for side-by-side validation.

## Build & Publish

```bash
dotnet publish -c Release -r linux-x64 --self-contained false -o publish/
```

## Deploy to TAS

```bash
# 1. Create the Redis service instance (adjust plan/service name for your foundation). If adjusting, don't forget to
# update manifest.yml
cf create-service redislabs small-redis redis-enterprise-service

# 2. Push the app
cf push

# 3. Verify
cf apps
cf services
```

## Validate Connectivity

```bash
APP_URL=$(cf app dotnet-redis-demo | grep routes | awk '{print $2}')

# Connection info — compare with Java app output
curl -s https://$APP_URL/api/items/info | jq .

# Lazy create
curl -s "https://$APP_URL/api/items/create?id=100&name=dotnet-test&desc=from-csharp" | jq .

# Read all — should show items from both Java and .NET apps
curl -s https://$APP_URL/api/items | jq .
```

## Cross-Runtime Validation

Since both apps share the same key structure and bind to the same service:

1. Create items via the **Java** app
2. Read them back via the **.NET** app (and vice versa)

Note: `/api/items/info`, may not work due to admin mode being required.

## Migration to Credhub Service

1. Same approach as the Java app — create a Credhub service. For migration, you might receive a new set of credentials 
   to use. For this test, we'll assume the goal is to  connect to the same redis instance.  Extract the credentials from 
   the existing Redis Broker Service.  The minimum fields needed are `host`,`password` and `port`.

   ```bash 
      cf env 
      # Example only output!
      #
      #     System-Provided:
      #      VCAP_SERVICES: {
      #         "redislabs": [
      #            {
      #               "binding_guid": "0c8bddf1-b73f-4a58-a98d-0179b950a931",
      #               "binding_name": null,
      #               "credentials": {
      #                   "host": "redis-16917.internal.redis-enterprise-01.xx.com",
      #                   "ip_list": [
      #                      "x.x.x.x"
      #                   ],
      #                  "name": "cf-0f523d65-04dd-46f5-90d8-7155e144286e",
      #                  "password": "<SOMETHINGSECRET>",
      #                  "port": 16917,
      #                  "sentinel_addrs": [
      #                     "redis-enterprise-01.xx.com"
      #                  ],
      #                  "sentinel_port": 8001
      #               },
      #               "instance_guid": "0f523d65-04dd-46f5-90d8-7155e144286e",
      #               "instance_name": "redis-enterprise-service",
      #               "label": "redislabs",
      #               "name": "redis-enterprise-service",
      #               "plan": "small-redis",
      #               "provider": null,
      #               "syslog_drain_url": null,
      #               "tags": [
      #                  "redislabs",
      #                  "redis"
      #               ],
      #               "volume_mounts": []
      #            }
      #         ]
      #      }
   
   
   ```

2. Create a **Credhub** service pointing to your Redis Enterprise endpoint: (Take note of the **-t 'redis'** parameter)
   ```bash
   cf create-service credhub default redis-creds-dotnet -t 'redis' -c '{
     "host": "redis-16917.internal.redis-enterprise-01.xx.com","port": 16917,"password":"<SOMETHINGSECRET>"
   }'
   ```
3. Redeploy: `cf push -f manifest-credhub.yml`

4. Hit `/api/items` to confirm you're now connected to Redis Enterprise via the Credhub Service (check the Redis item data is the same between the two apps).
   ```bash
   APP_URL=$(cf app dotnet-credhub-redis-demo | grep routes | awk '{print $2}')
   
   # Get the list of items
   curl -s https://$APP_URL/api/items | jq .
   ```

5. Run the same CRUD operations to validate functional parity.

# Contributions
Thanks to @rabeyta for his time on pairing on this.

