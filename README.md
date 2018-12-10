Under construction

## Dependencies:
- Kubernetes + Istio
- Cockroachdb
- Agones


## Cockroachdb

https://www.cockroachlabs.com/docs/stable/install-cockroachdb.html


- cockroach start --insecure --listen-addr=0.0.0.0:8888
- cockroach sql --insecure -p 8888

 ````
 CREATE USER IF NOT EXISTS speeddateuser; 
 CREATE DATABASE speeddate; 
 GRANT ALL ON DATABASE speeddate TO speeddateuser;
 ````
