
## Google Analytics Data Process

Grabs the realtime active user and page views for a site and submit the data to your ElasticSearch server.

### Setup

You need to have a config folder with the following

	/home/GA/config
	- appsettings.json
	- analytics.p12 # From Google Console


#### Example appsettings.json
	{
	  "ElasticSearchUrl": "http://logs.example.com:9200/YourIndexName/activedata/_bulk",
	  "GAID": "Analytics Profile ID eg. 12345678",
	  "ServiceAccountEmail": "xxxxxxx@analytics-xxxxxx.iam.gserviceaccount.com",
	  "IntervalMs": 60000
	}
#### Run

	docker run -v /home/GA/config:/app/config cadab/googleanalyticdataprocess

#### Troubleshooting
##### Create a Google Console Service Account for Google Analytics: [Click here to create Service Account](https://console.cloud.google.com/iam-admin/serviceaccounts)
##### Make sure to add your Google Console Service Account to your User Management permissions in Google Analytics
##### Where to find your Google Analytics Profile View ID
![Google Analytics Settings](https://reflectivedata.com/wp-content/uploads/2017/11/Screenshot-from-2017-11-20-12-05-20.png)