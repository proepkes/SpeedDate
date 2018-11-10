package service

import (
	"encoding/json"

	u "github.com/satori/go.uuid"
)

// Profile of a user in the server
type Profile struct {
	ID          u.UUID `json:"id"`
	Username    string `json:"username"`
	DisplayName string `json:"displayName"`
	Online      bool   `json:"online"`
}

func (p Profile) String() string {
	b, err := json.Marshal(p)
	if err != nil {
		return "unsupported value type"
	}
	return string(b)
}
