﻿/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System;
using UnityEngine.Assertions;
using UnityEngine;
using Oculus.Interaction;

namespace Oculus.Interaction
{
    public class AudioPhysics : MonoBehaviour
    {
        [Tooltip("Add a reference to the rigidbody on this gameobject.")]
        [SerializeField]
        private Rigidbody _rigidbody;

        [Tooltip("Reference an audio trigger instance for soft and hard collisions.")]
        [SerializeField]
        private ImpactAudio _impactAudioEvents;
        [Tooltip("Collisions below this value will play a soft audio event, and collisions above will play a hard audio event.")]
        [Range(0.0f, 8.0f)]
        [SerializeField]
        private float _velocitySplit = 1.0f;

        [Tooltip("Collisions below this value will be ignored and will not play audio.")]
        [Range(0.0f, 2.0f)]
        [SerializeField]
        private float _minimumVelocity = 0;

        [Tooltip("The shortest amount of time in seconds between collisions. Used to cull multiple fast collision events.")]
        [Range(0.0f, 2.0f)]
        [SerializeField]
        private float _timeBetweenCollisions = 0.2f;

        [Tooltip("By default (false), when two physics objects collide with physics audio components, we only play the one with the higher velocity." +
            "Setting this to true will allow both impacts to play.")]
        [SerializeField]
        private bool _allowMultipleCollisions = false;
        private float _timeAtLastCollision = 0f;

        protected virtual void Start()
        {
            Assert.IsNotNull(_impactAudioEvents.SoftCollisionSound, "AudioPhysics component has no audio soft collision audio trigger assigned");
            Assert.IsNotNull(_impactAudioEvents.HardCollisionSound, "AudioPhysics component has no audio hard collision audio trigger assigned");
            Assert.IsNotNull(_rigidbody, "AudioPhysics component has no rigidbody assigned");
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            TryPlayCollisionAudio(collision, _rigidbody);
        }

        private void TryPlayCollisionAudio(Collision collision, Rigidbody rigidbody)
        {
            // capture the velocity of the impact. TODO lets see if there is another way to get this data that might be better
            float collisionMagnitude = collision.relativeVelocity.sqrMagnitude;

            // make sure the gameobject we collided with is valid
            if (collision.collider.gameObject == null)
            {
                return;
            }

            // cull all the collisions we want to ignore

            // ignore any physics impacts that happen too close together
            float deltaTime = Time.time - _timeAtLastCollision;
            if (_timeBetweenCollisions > deltaTime)
            {
                return;
            }

            // only play a single sound when two physics objects collide

            if (_allowMultipleCollisions == false)
            {
                // check the object we collided with if it has an audio physics component
                if (collision.collider.gameObject.TryGetComponent(out AudioPhysics otherAudioPhysicsObj))
                {
                    if (GetObjectVelocity(otherAudioPhysicsObj) > GetObjectVelocity(this))
                    {
                        return;
                    }
                }
            }

            // update time variable for impacts too close together
            _timeAtLastCollision = Time.time;

            // play the audio
            PlayCollisionAudio(_impactAudioEvents, collisionMagnitude);
        }

        private void PlayCollisionAudio(ImpactAudio impactAudio, float magnitude)
        {
            // early out if there is no physics audio available
            if (impactAudio.HardCollisionSound == null || impactAudio.SoftCollisionSound == null)
            {
                return;
            }

            // cull audio by minimum velocity value
            if (magnitude > _minimumVelocity)
            {
                // play the hard or soft sound determined by the slider
                if (magnitude > _velocitySplit && impactAudio.HardCollisionSound != null)
                {
                    PlaySoundOnAudioTrigger(impactAudio.HardCollisionSound);
                }
                else
                {
                    PlaySoundOnAudioTrigger(impactAudio.SoftCollisionSound);
                }
            }
        }

        private static float GetObjectVelocity(AudioPhysics target)
        {
            return target._rigidbody.velocity.sqrMagnitude;
        }

        private void PlaySoundOnAudioTrigger(AudioTrigger audioTrigger)
        {
            if (audioTrigger != null)
            {
                audioTrigger.PlayAudio();
            }
        }
    }

    [Serializable]
    public struct ImpactAudio
    {
        [Tooltip("Hard collision sound will play when impact velocity is above the velocity split value.")]
        [SerializeField]
        private AudioTrigger _hardCollisionSound;
        [Tooltip("Soft collision sound will play when impact velocity is below the velocity split value.")]
        [SerializeField]
        private AudioTrigger _softCollisionSound;
        public AudioTrigger HardCollisionSound => _hardCollisionSound;
        public AudioTrigger SoftCollisionSound => _softCollisionSound;
    }
}
