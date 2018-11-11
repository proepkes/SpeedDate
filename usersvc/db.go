package usersvc

import (
	"fmt"

	"github.com/jinzhu/gorm"
	"github.com/satori/go.uuid"

	// Import GORM-related packages.
	_ "github.com/jinzhu/gorm/dialects/postgres"
)

// User is our model, which corresponds to the "users" database table.
type User struct {
	ID   uuid.UUID `gorm:"primary_key" json:"id" sql:"id UUID PRIMARY KEY DEFAULT gen_random_uuid()"`
	Name string    `json:"name"`
}

// ErrNotFound is the error returned when attempting to load a record that does
// not exist.
var ErrNotFound = fmt.Errorf("record not found")

// Cockroach is the database driver.
type Cockroach struct {
	// client is the Cockroach client.
	client *gorm.DB
}

// NewCockroachDB creates a Cockroach DB database driver given an underlying client.
func NewCockroachDB(client *gorm.DB) (*Cockroach, error) {
	return &Cockroach{client}, nil
}

// CreateUser stores a new user
func (c *Cockroach) CreateUser(data interface{}) error {
	res := c.client.Create(data)
	if res.Error != nil {
		return res.Error
	}
	return nil
}

// GetUser reads a record by ID. data is unmarshaled into and should hold a pointer.
func (c *Cockroach) GetUser(id string, data interface{}) error {
	res := c.client.Where("id = ?", id).First(data)
	if res.Error != nil {
		if res.Error.Error() == ErrNotFound.Error() {
			return ErrNotFound
		}
		return res.Error
	}
	return nil
}
