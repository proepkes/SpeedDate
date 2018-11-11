cd $GOPATH/src/github.com/proepkes/speeddate/usersvc
goa gen github.com/proepkes/speeddate/usersvc/design
# goa example speeddate/usersvc/design


cd $GOPATH/src/github.com/proepkes/speeddate/authsvc
goa gen github.com/proepkes/speeddate/authsvc/design
goa example github.com/proepkes/speeddate/authsvc/design

docker build --tag=proepkes/usersvc:dev .