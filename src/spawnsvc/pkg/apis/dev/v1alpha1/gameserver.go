package v1alpha1

import (
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/apis/dev"
	corev1 "k8s.io/api/core/v1"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
)

type State string

const (
	Requested State = "Requested"
	Ready     State = "Ready"

	GameServerPodLabel = dev.GroupName + "_gameserver"
)

// +genclient
// +genclient:noStatus
// +k8s:deepcopy-gen:interfaces=k8s.io/apimachinery/pkg/runtime.Object
type GameServer struct {
	metav1.TypeMeta   `json:",inline"`
	metav1.ObjectMeta `json:"metadata,omitempty"`

	State State `json:"state"`
	Port  int32 `json:"Port"`

	Template corev1.PodTemplateSpec `json:"template"`
}

// +k8s:deepcopy-gen:interfaces=k8s.io/apimachinery/pkg/runtime.Object
type GameServerList struct {
	metav1.TypeMeta `json:",inline"`
	metav1.ListMeta `json:"metadata,omitempty"`

	Items []GameServer `json:"items"`
}

func NewGameserver() *GameServer {
	gs := &GameServer{
		ObjectMeta: metav1.ObjectMeta{
			GenerateName: "gameserver",
			Namespace:    "default",
		},
		State: Requested,
		Template: corev1.PodTemplateSpec{
			Spec: corev1.PodSpec{
				Containers: []corev1.Container{{Name: "exampleserver", Image: "docker.io/proepkes/mmsvc:dev"}},
			},
		},
	}
	gs.ObjectMeta.Finalizers = append(gs.ObjectMeta.Finalizers, dev.GroupName)

	return gs
}

func (gs *GameServer) Pod() *corev1.Pod {
	pod := &corev1.Pod{
		ObjectMeta: *gs.Template.ObjectMeta.DeepCopy(),
		Spec:       *gs.Template.Spec.DeepCopy(),
	}
	pod.ObjectMeta.Labels = map[string]string{
		GameServerPodLabel: gs.ObjectMeta.Name,
	}
	pod.ObjectMeta.Name = gs.ObjectMeta.Name

	// Set ownerRef because IsControlledBy checks if the object has a controllerRef set to the given owner
	ref := metav1.NewControllerRef(gs, SchemeGroupVersion.WithKind("GameServer"))
	pod.ObjectMeta.OwnerReferences = append(pod.ObjectMeta.OwnerReferences, *ref)
	return pod
}
