cd $GOPATH/speeddate/usersvc
goa gen github.com/proepkes/speeddate/usersvc/design
# goa example speeddate/usersvc/design


docker build --tag=proepkes/usersvc:dev .