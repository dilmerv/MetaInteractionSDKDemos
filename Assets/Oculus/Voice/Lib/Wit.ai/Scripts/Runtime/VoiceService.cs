﻿/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Facebook.WitAi.Configuration;
using Facebook.WitAi.Events;
using Facebook.WitAi.Interfaces;
using UnityEngine;

namespace Facebook.WitAi
{
    public abstract class VoiceService : MonoBehaviour, IVoiceService
    {
        [Tooltip("Events that will fire before, during and after an activation")] [SerializeField]
        public VoiceEvents events = new VoiceEvents();

        /// <summary>
        /// Returns true if this voice service is currently active and listening with the mic
        /// </summary>
        public abstract bool Active { get; }

        /// <summary>
        /// Returns true if the service is actively communicating with Wit.ai during an Activation. The mic may or may not still be active while this is true.
        /// </summary>
        public abstract bool IsRequestActive { get; }

        /// <summary>
        /// Gets/Sets a custom transcription provider. This can be used to replace any built in asr
        /// with an on device model or other provided source
        /// </summary>
        public abstract ITranscriptionProvider TranscriptionProvider { get; set; }

        /// <summary>
        /// Returns true if this voice service is currently reading data from the microphone
        /// </summary>
        public abstract bool MicActive { get; }

        public VoiceEvents VoiceEvents
        {
            get => events;
            set => events = value;
        }

        /// <summary>
        /// Returns true if the audio input should be read in an activation
        /// </summary>
        protected abstract bool ShouldSendMicData { get; }

        /// <summary>
        /// Start listening for sound or speech from the user and start sending data to Wit.ai once sound or speech has been detected.
        /// </summary>
        public abstract void Activate();

        /// <summary>
        /// Activate the microphone and send data for NLU processing. Includes optional additional request parameters like dynamic entities and maximum results.
        /// </summary>
        /// <param name="requestOptions"></param>
        public abstract void Activate(WitRequestOptions requestOptions);

        /// <summary>
        /// Activate the microphone and send data for NLU processing immediately without waiting for sound/speech from the user to begin.
        /// </summary>
        public abstract void ActivateImmediately();

        /// <summary>
        /// Activate the microphone and send data for NLU processing immediately without waiting for sound/speech from the user to begin.  Includes optional additional request parameters like dynamic entities and maximum results.
        /// </summary>
        public abstract void ActivateImmediately(WitRequestOptions requestOptions);

        /// <summary>
        /// Stop listening and submit any remaining buffered microphone data for processing.
        /// </summary>
        public abstract void Deactivate();

        /// <summary>
        /// Stop listening and abort any requests that may be active without waiting for a response.
        /// </summary>
        public abstract void DeactivateAndAbortRequest();

        /// <summary>
        /// Send text data for NLU processing. Results will return the same way a voice based activation would.
        /// </summary>
        /// <param name="text"></param>
        public abstract void Activate(string text);

        /// <summary>
        /// Send text data for NLU processing with custom request options.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="requestOptions"></param>
        public abstract void Activate(string text, WitRequestOptions requestOptions);
    }

    public interface IVoiceService
    {
        /// <summary>
        /// Returns true if this voice service is currently active and listening with the mic
        /// </summary>
        bool Active { get; }

        bool IsRequestActive { get; }

        bool MicActive { get; }

        VoiceEvents VoiceEvents { get; set; }

        ITranscriptionProvider TranscriptionProvider { get; set; }

        /// <summary>
        /// Activate the microphone and send data for NLU processing.
        /// </summary>
        void Activate();

        /// <summary>
        /// Activate the microphone and send data for NLU processing with custom request options.
        /// </summary>
        /// <param name="requestOptions"></param>
        void Activate(WitRequestOptions requestOptions);

        void ActivateImmediately();
        void ActivateImmediately(WitRequestOptions requestOptions);

        /// <summary>
        /// Stop listening and submit the collected microphone data for processing.
        /// </summary>
        void Deactivate();

        /// <summary>
        /// Stop listening and abort any requests that may be active without waiting for a response.
        /// </summary>
        void DeactivateAndAbortRequest();

        /// <summary>
        /// Send text data for NLU processing
        /// </summary>
        /// <param name="text"></param>
        void Activate(string transcription);

        /// <summary>
        /// Send text data for NLU processing with custom request options.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="requestOptions"></param>
        void Activate(string text, WitRequestOptions requestOptions);

    }
}
