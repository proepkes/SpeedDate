Under construction

https://www.cockroachlabs.com/docs/stable/install-cockroachdb.html


cockroach sql --insecure -p 8888
 
 CREATE USER IF NOT EXISTS speeddateuser;
 CREATE DATABASE speeddate;
 GRANT ALL ON DATABASE speeddate TO speeddateuser;