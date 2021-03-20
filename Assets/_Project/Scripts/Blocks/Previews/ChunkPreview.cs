using System;
using System.Collections.Generic;
using System.Linq;
using Blocks.Sockets;
using ElasticSea.Framework.Extensions;
using UnityEngine;

namespace Blocks.Previews
{
    public class ChunkPreview : MonoBehaviour
    {
        [SerializeField] private Chunk owner;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Material material;
        [SerializeField] private Dictionary<Socket, Socket> previewToRealSocketMap;
        [SerializeField] private PreviewConnector connector;
        
        public IEnumerable<SocketPair> Snap { get; private set; }

        private void Update()
        {
            var (state, socketPairs) = connector.CheckForConnection(owner);
            Visible = true;
            Snap = MapToRealChunk(socketPairs);

            switch (state)
            {
                case State.NoConnections:
                    material.color = Color.green.SetAlpha(.3f);
                    break;
                case State.BlockingWithItself:
                    material.color = Color.red.SetAlpha(.3f);
                    break;
                case State.SocketsTooFar:
                    material.color = Color.yellow.SetAlpha(.3f);
                    break;
                case State.OneSocketAligned:
                    material.color = Color.magenta.SetAlpha(.3f);
                    break;
                case State.Aligned:
                    material.color = Color.blue.SetAlpha(.3f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IEnumerable<SocketPair> MapToRealChunk(IEnumerable<SocketPair> connectionCandidates)
        {
            return connectionCandidates.Select(socketPair =>
            {
                var thisSocket = previewToRealSocketMap[socketPair.This];
                var otherSocket = socketPair.Other;
                return new SocketPair{This = thisSocket, Other = otherSocket};
            });
        }
      
        public void BeginSnap()
        {
            gameObject.SetActive(true);
            SwitchLayerInChildren(transform, "Default", "Snap");
            Update();
        }

        public void EndSnap()
        {
            if (Snap.Any())
            {
                // Copy the transform back
                owner.transform.CopyWorldFrom(transform);
                
                var newBlock = ChunkFactory.Connect(Snap);
                SwitchLayerInChildren(newBlock.transform, "Snap", "Default");
            }
            else
            {
                transform.GetComponent<Rigidbody>().isKinematic = false;
                SwitchLayerInChildren(transform, "Snap", "Default");
                gameObject.SetActive(false);
            }
        }

        private bool? visible;

        public bool Visible
        {
            get => visible.Value;
            set
            {
                if (value != visible)
                {
                    foreach (var renderer in renderers)
                    {
                        renderer.enabled = value;
                    }
                    
                    visible = value;
                }
            }
        }

        public Chunk Owner
        {
            set => owner = value;
        }

        public Renderer[] Renderers
        {
            set => renderers = value;
        }

        public Material Material
        {
            set => material = value;
        }

        public Dictionary<Socket, Socket> PreviewToRealSocketMap
        {
            set => previewToRealSocketMap = value;
        }

        public PreviewConnector Connector
        {
            set => connector = value;
        }

        private void SwitchLayerInChildren(Transform target, string from, string to)
        {
            foreach (var child in target.GetComponentsInChildren<Transform>())
            {
                if (child.gameObject.layer == LayerMask.NameToLayer(from))
                {
                    child.gameObject.layer = LayerMask.NameToLayer(to);
                }
            }
        }
    }
}