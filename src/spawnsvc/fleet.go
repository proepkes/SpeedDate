package spawnsvc

import (
	"context"
	"fmt"
	"log"
	"strconv"

	fleet "github.com/proepkes/speeddate/src/spawnsvc/gen/fleet"

	"agones.dev/agones/pkg/apis/stable/v1alpha1"
	"agones.dev/agones/pkg/client/clientset/versioned"
	corev1 "k8s.io/api/core/v1"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/client-go/kubernetes"
	"k8s.io/client-go/rest"
)

// fleet service example implementation.
// The example methods log the requests and return zero values.
type fleetSvc struct {
	logger              *log.Logger
	speeddateNamespace  string
	gameserverNamespace string
	k8sClient           kubernetes.Interface
	client              *versioned.Clientset
}

// NewFleet returns the fleet service implementation.
func NewFleet(logger *log.Logger, speeddateNamespace string, gameserverNamespace string, clusterConfig *rest.Config) fleet.Service {

	k8sClient, err := kubernetes.NewForConfig(clusterConfig)
	if err != nil {
		logger.Fatal("Could not create the k8s clientset")
	}

	client, err := versioned.NewForConfig(clusterConfig)
	if err != nil {
		logger.Fatal("Could not create the agones api clientset")
	}

	x := &fleetSvc{logger, speeddateNamespace, gameserverNamespace, k8sClient, client}
	logger.Println(x)
	return x
}

// Add a new gameserver.
func (s *fleetSvc) Add(ctx context.Context) (res string, err error) {
	s.logger.Print("fleet.add")

	cm, err := s.getGameserverConfig()
	if err != nil {
		s.logger.Println(err.Error())
		return "", err
	}

	cp, err := strconv.ParseInt(cm.Data["ContainerPort"], 10, 32)
	if err != nil {
		s.logger.Println(err.Error())
		return "", err
	}

	// Create a GameServer
	gs := &v1alpha1.GameServer{
		ObjectMeta: metav1.ObjectMeta{GenerateName: cm.Data["GameserverNamePrefix"], Namespace: cm.Data["GameserverNamespace"]},
		Spec: v1alpha1.GameServerSpec{
			Ports: []v1alpha1.GameServerPort{{ContainerPort: int32(cp), PortPolicy: v1alpha1.PortPolicy(cm.Data["PortPolicy"])}},
			Template: corev1.PodTemplateSpec{
				Spec: corev1.PodSpec{
					Containers: []corev1.Container{{Name: cm.Data["ContainerName"], Image: cm.Data["ContainerImage"]}},
				},
			},
		},
	}
	newGS, err := s.client.StableV1alpha1().GameServers(cm.Data["GameserverNamespace"]).Create(gs)
	if err != nil {
		s.logger.Println(err.Error())
		panic(err)
	}

	fmt.Printf("New game servers' name is: %s", newGS.ObjectMeta.Name)

	return
}

// Create a new fleet.
func (s *fleetSvc) Create(ctx context.Context, p *fleet.Fleet) (res string, err error) {
	f, err := s.client.StableV1alpha1().Fleets(p.ObjectMeta.Namespace).Create(asFleet(p))
	if err != nil {
		s.logger.Println(err.Error())
		return "", err
	}

	return fmt.Sprint(f), nil
}

func asFleet(p *fleet.Fleet) *v1alpha1.Fleet {
	return &v1alpha1.Fleet{
		ObjectMeta: metav1.ObjectMeta{GenerateName: p.ObjectMeta.GenerateName, Namespace: p.ObjectMeta.Namespace},
		Spec: v1alpha1.FleetSpec{
			Replicas: p.FleetSpec.Replicas,
			Template: v1alpha1.GameServerTemplateSpec{
				Spec: asGameServerSpec(p.FleetSpec.Template.GameServerSpec),
			},
		},
	}
}

func asGameServerSpec(p *fleet.GameServerSpec) v1alpha1.GameServerSpec {
	return v1alpha1.GameServerSpec{
		Container: p.ContainerName,
		Ports: []v1alpha1.GameServerPort{{
			ContainerPort: p.ContainerPort,
			Name:          "gameport",
			PortPolicy:    v1alpha1.PortPolicy(p.PortPolicy),
			Protocol:      corev1.ProtocolUDP,
		}},
		Template: corev1.PodTemplateSpec{
			Spec: corev1.PodSpec{
				Containers: []corev1.Container{{
					Name:            p.ContainerName,
					Image:           p.ContainerImage,
					ImagePullPolicy: corev1.PullIfNotPresent,
				}},
			},
		},
	}
}

// Delete a fleet
func (s *fleetSvc) Delete(ctx context.Context, p *fleet.DeletePayload) (err error) {
	s.logger.Print("fleet.delete")

	err = s.client.StableV1alpha1().Fleets(p.Namespace).Delete(p.Name, &metav1.DeleteOptions{})
	if err != nil {
		s.logger.Println(err.Error())
		return err
	}

	return
}

// List all fleets.
func (s *fleetSvc) List(ctx context.Context, p *fleet.ListPayload) (res fleet.StoredFleetCollection, err error) {
	s.logger.Print("fleet.list")
	fleets, err := s.client.StableV1alpha1().Fleets(p.Namespace).List(metav1.ListOptions{})
	if err != nil {
		s.logger.Println(err.Error())
		return nil, err
	}
	for _, f := range fleets.Items {
		s := f.Spec.Template.Spec //Equals GameServerSpec
		res = append(res, &fleet.StoredFleet{
			Name: f.Name,
			ObjectMeta: &fleet.ObjectMeta{
				Namespace:    f.ObjectMeta.Namespace,
				GenerateName: f.ObjectMeta.GenerateName,
			},
			FleetSpec: &fleet.FleetSpec{
				Replicas: f.Spec.Replicas,
				Template: &fleet.GameserverTemplate{
					GameServerSpec: &fleet.GameServerSpec{
						PortPolicy:     string(s.Ports[0].PortPolicy),
						ContainerPort:  s.Ports[0].ContainerPort,
						ContainerImage: s.Template.Spec.Containers[0].Image,
						ContainerName:  s.Template.Spec.Containers[0].Name,
					},
				},
			},
			FleetStatus: &fleet.FleetStatus{
				Replicas:          f.Status.Replicas,
				ReadyReplicas:     f.Status.ReadyReplicas,
				AllocatedReplicas: f.Status.AllocatedReplicas,
			},
		})
	}
	return
}

// Removes all gameserver pods.
func (s *fleetSvc) Clear(ctx context.Context) (res string, err error) {
	s.logger.Print("fleet.clear")
	cm, err := s.getGameserverConfig()
	if err != nil {
		s.logger.Println(err.Error())
		return "", err
	}

	s.client.StableV1alpha1().GameServers(cm.Data["GameserverNamespace"]).DeleteCollection(&metav1.DeleteOptions{}, metav1.ListOptions{})
	return
}

// Get gameserver deployment configuration.
func (s *fleetSvc) Configuration(ctx context.Context) (res *fleet.Fleet, err error) {
	cm, err := s.getGameserverConfig()
	if err != nil {
		s.logger.Println(err.Error())
		return nil, err
	}

	cp, _ := strconv.Atoi(cm.Data["ContainerPort"])
	rp, _ := strconv.Atoi(cm.Data["Replicas"])

	res = &fleet.Fleet{
		ObjectMeta: &fleet.ObjectMeta{
			Namespace:    cm.Data["Namespace"],
			GenerateName: cm.Data["NamePrefix"],
		},
		FleetSpec: &fleet.FleetSpec{
			Replicas: int32(rp),
			Template: &fleet.GameserverTemplate{
				ObjectMeta: &fleet.ObjectMeta{
					GenerateName: cm.Data["GameserverNamePrefix"],
					Namespace:    cm.Data["Namespace"],
				},
				GameServerSpec: &fleet.GameServerSpec{
					ContainerName:  cm.Data["ContainerName"],
					ContainerPort:  int32(cp),
					ContainerImage: cm.Data["ContainerImage"],
					PortPolicy:     cm.Data["PortPolicy"],
				},
			},
		},
	}
	s.logger.Print("fleet.configuration")

	return
}

// Configure gameserver deployment.
func (s *fleetSvc) Configure(ctx context.Context, p *fleet.ConfigurePayload) (res string, err error) {
	s.logger.Print("fleet.configure")

	cm, err := s.getGameserverConfig()
	if err != nil {
		s.logger.Println(err.Error())
		return "", err
	}

	cmCopy := cm.DeepCopy()
	cmCopy.Data["ContainerImage"] = p.ContainerImage
	cmCopy.Data["ContainerName"] = p.ContainerName
	cmCopy.Data["ContainerPort"] = string(p.ContainerPort)
	cmCopy.Data["NamePrefix"] = p.NamePrefix
	cmCopy.Data["GameserverNamePrefix"] = p.GameserverNamePrefix
	cmCopy.Data["Namespace"] = p.Namespace
	cmCopy.Data["Replicas"] = string(p.Replicas)

	_, err = s.k8sClient.CoreV1().ConfigMaps(s.speeddateNamespace).Update(cmCopy)
	if err != nil {
		s.logger.Println(err.Error())
		return "", err
	}
	return
}

func (s *fleetSvc) getGameserverConfig() (*corev1.ConfigMap, error) {
	configMaps, err := s.k8sClient.CoreV1().ConfigMaps(s.speeddateNamespace).List(metav1.ListOptions{})
	if err != nil {
		s.logger.Println(err.Error())
		return nil, err
	}
	for _, cm := range configMaps.Items {
		if cm.Name == "gameserver-config" {
			return &cm, nil
		}
	}

	return nil, fmt.Errorf("Could not find gameserver-config in namespace %s", s.speeddateNamespace)
}
