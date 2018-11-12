/*
 * Genarate rsa keys.
   https://gist.github.com/rajulbhatnagar/b9f5f3214978caf4532a8ab5cb116977
*/

package main

import (
	"crypto/ecdsa"
	"crypto/elliptic"
	"crypto/rand"
	"crypto/x509"
	"encoding/pem"
	"fmt"
	"log"
	"os"
)

// You can also verify the generated key files with openssl
// openssl rsa -inform PEM -pubin -in pubkey.pem
// openssl rsa -inform PEM -in privateKey.pem
func main() {

	key, err := ecdsa.GenerateKey(elliptic.P256(), rand.Reader)
	if err != nil {
		log.Fatal(err)
	}
	checkError(err)

	pemKeyPair("secret.key", key)
}

func pemKeyPair(fileName string, key *ecdsa.PrivateKey) (privKeyPEM []byte, pubKeyPEM []byte, err error) {
	der, err := x509.MarshalECPrivateKey(key)
	if err != nil {
		return nil, nil, err
	}

	outFile, err := os.Create(fileName)
	defer outFile.Close()
	pem.Encode(outFile, &pem.Block{
		Type:  "EC PRIVATE KEY",
		Bytes: der,
	})

	der, err = x509.MarshalPKIXPublicKey(key.Public())
	if err != nil {
		return nil, nil, err
	}

	outFilePub, err := os.Create(fileName + ".pub")
	defer outFilePub.Close()
	pem.Encode(outFilePub, &pem.Block{
		Type:  "EC PUBLIC KEY",
		Bytes: der,
	})

	return
}

func checkError(err error) {
	if err != nil {
		fmt.Println("Fatal error ", err.Error())
		os.Exit(1)
	}
}
