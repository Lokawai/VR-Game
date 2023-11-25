using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    /// <summary>
    /// Used with GenericObjects to randomly move them around
    /// </summary>
    public class IndependentMover : NetworkBehaviour
    {
        public static bool EnableVerboseDebug;
        private Vector3 m_Direction;
        private Rigidbody m_Rigidbody;

        private void VerboseDebug(string message)
        {
            if (EnableVerboseDebug)
            {
                Debug.Log(message);
            }
        }

        public override void OnNetworkSpawn()
        {
            VerboseDebug($"{nameof(IndependentMover)} NID: {NetworkObjectId}");
            m_Rigidbody = GetComponent<Rigidbody>();
            if (NetworkObject != null && m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = !NetworkObject.IsOwner;
                if (!m_Rigidbody.isKinematic)
                {
                    ChangeDirection(true, true);
                }
            }
        }

        private void FixedUpdate()
        {
            if (IsServer && IsOwner)
            {
                Move(4);
            }
        }

        public void Move(int speed)
        {
            if (m_Rigidbody == null)
            {
                m_Rigidbody = GetComponent<Rigidbody>();
            }
            if (m_Rigidbody != null)
            {
                m_Rigidbody.MovePosition(transform.position + m_Direction * (speed * Time.fixedDeltaTime));
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (collision.gameObject.CompareTag("Floor") || collision.gameObject.CompareTag("GenericObject"))
            {
                return;
            }
            Vector3 collisionPoint = collision.collider.ClosestPoint(transform.position);
            bool moveRight = collisionPoint.x < transform.position.x;
            bool moveDown = collisionPoint.z > transform.position.z;
            ChangeDirection(moveRight, moveDown);
        }

        private void ChangeDirection(bool moveRight, bool moveDown)
        {
            float ang = Random.Range(0, 2 * Mathf.PI);

            m_Direction.x = Mathf.Cos(ang);
            m_Direction.y = 0.0f;
            ang = Random.Range(0, 2 * Mathf.PI);
            m_Direction.z = Mathf.Sin(ang);
            m_Direction.Normalize();
        }
    }
}
