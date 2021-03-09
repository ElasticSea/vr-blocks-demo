using System;
using System.Collections.Generic;
using ElasticSea.Framework.Extensions;
using UnityEngine;

namespace Blocks
{
    public static class ShadowExtensions
    {
        public static (Vector3 position, Quaternion rotation, bool valid) GetShadowAlign(this SnapPreview shadow, Chunk chunkSource)
        {
            var connections = chunkSource.GetConnections();
            connections = FilterOutCollinear(connections);

            // Chose two closes connections and choose origin and alignment.
            // If only one connection is available use that one.
            if (connections.Length == 0)
            {
                return (default, default, false);
            }

            if (connections.Length == 1)
            {
                var thisSocket = connections[0].thisSocket;
                var otherSocket = connections[0].otherSocket;

                var (position1, rotation1) = shadow.GetShadowAlign(thisSocket, otherSocket, chunkSource.transform);
                return (position1, rotation1, true);
            }

            var thisSocketA = connections[0].thisSocket;
            var otherSocketA = connections[0].otherSocket;
            var thisSocketB = connections[1].thisSocket;
            var otherSocketB = connections[1].otherSocket;

            var (position, rotation) =  shadow.GetShadowAlign(thisSocketA, thisSocketB, otherSocketA, otherSocketB, chunkSource.transform);
            return (position, rotation, true);
        }

        private static (Transform thisSocket, Transform otherSocket)[] FilterOutCollinear((Transform thisSocket, Transform otherSocket)[] connections)
        {
            var output = new List<(Transform thisSocket, Transform otherSocket)>();
            foreach (var connection1 in connections)
            {
                var isCollinear = false;
                foreach (var connection2 in connections)
                {
                    if (connection1 != connection2)
                    {
                        // Chose two closes connections and choose origin and alignment.
                        var thisSocketA = connection1.thisSocket;
                        var otherSocketA = connection1.otherSocket;
                        var thisSocketB = connection2.thisSocket;
                        var otherSocketB = connection2.otherSocket;
            
                        var directionA = otherSocketA.up;
                        var directionB = otherSocketA.position - otherSocketB.position;

                        // Check if the directions are collinear
                        if (Math.Abs(directionA.Dot(directionB)) > 0.0001f)
                        {
                            isCollinear = true;
                        }
                    }
                }

                if (isCollinear == false)
                {
                    output.Add(connection1);
                }
            }

            return output.ToArray();
        }

        private static (Vector3 position, Quaternion rotation) GetShadowAlign(this SnapPreview shadow, Transform thisA, Transform otherA, Transform blockSource)
        {
            var thisDir = thisA.right.normalized;
            var otherDir = otherA.right.normalized;
            
            return shadow.GetShadowAlign(thisA, thisDir, otherA, otherDir, blockSource);
        }

        private static (Vector3 position, Quaternion rotation) GetShadowAlign(this SnapPreview shadow, Transform thisA, Transform thisB, Transform otherA, Transform otherB, Transform blockSource)
        {
            var thisDir = (thisB.position - thisA.position).normalized;
            var otherDir = (otherB.position - otherA.position).normalized;

            return shadow.GetShadowAlign(thisA, thisDir, otherA, otherDir, blockSource);
        }
        
        private static (Vector3 position, Quaternion rotation) GetShadowAlign(this SnapPreview shadow, Transform thisA, Vector3 thisDir, Transform otherA, Vector3 otherDir, Transform blockSource)
        {
            const bool OLDWAY = false;
            
            var thisToOtherRotation = Quaternion.FromToRotation(thisDir, otherDir);

            // Correct for rotation along the direction (multiple valid states for resulting rotation)
            var correctedUpVector = thisToOtherRotation * -thisA.up;
            var angle = Vector3.SignedAngle(correctedUpVector, otherA.up, otherDir);
            var correction = Quaternion.AngleAxis(angle, otherDir);

            if (OLDWAY)
            {
                shadow.transform.rotation = correction * thisToOtherRotation * blockSource.rotation;
                
                shadow.transform.position = blockSource.position;

                // TODO Get the resulting position and rotation without touching the transforms
                var blockSocketLocalPosition = blockSource.InverseTransformPoint(thisA.position);
                var adjustedWorldPosition = shadow.transform.TransformPoint(blockSocketLocalPosition);
                var offset = otherA.position - adjustedWorldPosition;
            
                return (blockSource.position + offset, shadow.transform.rotation);
            }
            else
            {
                
                var targetRotation = correction * thisToOtherRotation * blockSource.rotation;

                // TODO Get the resulting position and rotation without touching the transforms
                var blockSocketLocalPosition = blockSource.InverseTransformPoint(thisA.position);
                var adjustedWorldPosition = (blockSource.position - shadow.transform.position) + shadow.transform.TransformPoint(blockSocketLocalPosition);
                var offset = otherA.position - adjustedWorldPosition;
            
                return (blockSource.position + offset, targetRotation);
            }
        }
    }
}