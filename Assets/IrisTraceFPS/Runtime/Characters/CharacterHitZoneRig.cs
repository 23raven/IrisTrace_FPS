using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Characters
{
    /// <summary>Maintains player hitboxes as the CharacterController changes height while crouching.</summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHitZoneRig : MonoBehaviour
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private SphereCollider headCollider;
        [SerializeField] private BoxCollider bodyCollider;
        [SerializeField] private BoxCollider legsCollider;

        private float lastHeight = -1f;
        private float lastRadius = -1f;
        private Vector3 lastCenter;

        public void Configure(CharacterController controller, SphereCollider head, BoxCollider body, BoxCollider legs)
        {
            characterController = controller;
            headCollider = head;
            bodyCollider = body;
            legsCollider = legs;
            RefreshHitZones(true);
        }

        private void LateUpdate()
        {
            RefreshHitZones(false);
        }

        private void RefreshHitZones(bool force)
        {
            if (characterController == null || headCollider == null || bodyCollider == null || legsCollider == null)
                return;

            float height = Mathf.Max(0.1f, characterController.height);
            float radius = Mathf.Max(0.05f, characterController.radius);
            Vector3 center = characterController.center;
            if (!force && Mathf.Approximately(height, lastHeight) && Mathf.Approximately(radius, lastRadius) && center == lastCenter)
                return;

            float bottom = center.y - height * 0.5f;
            float headRadius = Mathf.Min(radius * 0.65f, height * 0.12f);

            headCollider.radius = headRadius;
            headCollider.transform.localPosition = new Vector3(center.x, bottom + height * 0.88f, center.z);

            bodyCollider.size = new Vector3(radius * 2f, height * 0.47f, radius * 2f);
            bodyCollider.transform.localPosition = new Vector3(center.x, bottom + height * 0.54f, center.z);

            legsCollider.size = new Vector3(radius * 2f, height * 0.35f, radius * 2f);
            legsCollider.transform.localPosition = new Vector3(center.x, bottom + height * 0.18f, center.z);

            lastHeight = height;
            lastRadius = radius;
            lastCenter = center;
            // The boxes change size/position during a crouch transition; make the new hitboxes
            // available to weapon raycasts in the same frame.
            Physics.SyncTransforms();
        }
    }
}
