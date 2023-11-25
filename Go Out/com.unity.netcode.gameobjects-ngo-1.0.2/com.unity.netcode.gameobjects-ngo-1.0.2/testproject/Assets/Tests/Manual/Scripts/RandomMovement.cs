using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace TestProject.ManualTests
{

    /// <summary>
    /// Used with GenericObjects to randomly move them around
    /// </summary>
    public class RandomMovement : NetworkBehaviour, IPlayerMovement
    {
        private Vector3 m_Direction;
        private Rigidbody m_Rigidbody;

        private NetworkTransform m_NetworkTransform;
        private ClientNetworkTransform m_ClientNetworkTransform;

        public bool HasAuthority()
        {
            if (m_NetworkTransform != null)
            {
                if (m_ClientNetworkTransform == null)
                {
                    m_ClientNetworkTransform = m_NetworkTransform as ClientNetworkTransform;
                }
                if (m_ClientNetworkTransform != null)
                {
                    return IsOwner;
                }
            }
            return IsServer;
        }

        public override void OnNetworkSpawn()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_NetworkTransform = GetComponent<NetworkTransform>();
            if (NetworkObject != null && m_Rigidbody != null)
            {
                if (HasAuthority())
                {
                    ChangeDirection(true, true);
                }
            }
        }

        private Vector3 m_MoveTowardsPosition;
        private float m_CurrentSpeed;

        public void Move(int speed)
        {
            m_CurrentSpeed = speed;
        }


        // We just apply our current direction with magnitude to our current position during fixed update
        private void FixedUpdate()
        {
            if (!IsSpawned)
            {
                return;
            }
            if (HasAuthority())
            {
                if (m_Rigidbody == null)
                {
                    m_Rigidbody = GetComponent<Rigidbody>();
                }
                if (m_Rigidbody != null)
                {
                    var position = m_Rigidbody.position;
                    var yAxis = position.y;
                    position += (m_Direction * m_CurrentSpeed);
                    position.y = yAxis;
                    m_Rigidbody.position = Vector3.Lerp(m_Rigidbody.position, position, Time.fixedDeltaTime);
                }
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (HasAuthority())
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
