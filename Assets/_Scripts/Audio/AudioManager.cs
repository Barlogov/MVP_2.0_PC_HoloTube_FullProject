using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NAudio.Wave;
using System;
using UnityEngine.UI;
using System.Net.Sockets;
using NAudio.Wave.SampleProviders;
using NAudio.Mixer;
using System.Linq;
using System.Threading.Tasks;

public class AudioManager : MonoBehaviour
{
    [SerializeField] NetworkManager networkManager;

    [SerializeField] bool microphoneEnabled = false;
    [SerializeField] bool speakerEnabled = false;

    WaveFormat waveFormat = new WaveFormat(48000, 16, 1);
    WaveInEvent waveIn;
    WaveOutEvent waveOut;

    Dictionary<string, BufferedWaveProvider> waveProviders = new Dictionary<string, BufferedWaveProvider>();

    //WaveOut.DeviceCount 
    MixingSampleProvider mixer;
    

    void Start()
    {
        // Microphone
        waveIn = new WaveInEvent();
        waveIn.DeviceNumber = 0;
        waveIn.WaveFormat = waveFormat;
        waveIn.DataAvailable += WaveIn_DataAvailable;
        waveIn.RecordingStopped += WaveIn_RecordingStopped;

        // Speaker
        waveOut = new WaveOutEvent();


        mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(waveFormat.SampleRate, waveFormat.Channels));
        waveOut.Init(mixer);

        _ = Task.Run(PeriodicallyClearMixer);
    }

    //
    // Microphone
    //
    public void EnableMicrophone()
    {
        Debug.Log("Enable Microphone");
        microphoneEnabled = true;
        waveIn.StartRecording();
    }

    public void DisableMicrophone()
    {
        Debug.Log("Disable Microphone");
        microphoneEnabled = false;
        waveIn.StopRecording();
    }


    private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
    {
        Debug.Log("Recording Audio");
        try
        {
            networkManager.SendMicAudio(e.Buffer);
        }
        catch(Exception ex)
        {
            Debug.Log(ex.ToString());
        }
        
    }
    private void WaveIn_RecordingStopped(object? sender, StoppedEventArgs e)
    {
        Debug.Log("Audio Recording Stopped");
    }

    //
    // Speaker
    //
    public void EnableSpeaker()
    {
        speakerEnabled = true;

        foreach (BufferedWaveProvider waveProvider in waveProviders.Values)
        {
            waveProvider.ClearBuffer();
        }

        waveOut.Play();
    }
    public void DisableSpeaker()
    {
        speakerEnabled = false;

        foreach (BufferedWaveProvider waveProvider in waveProviders.Values)
        {
            waveProvider.ClearBuffer();
        }

        waveOut.Stop();
    }

    //
    // Audio Samples
    //
    public void ProvideAudioSample(byte[] receivedMessage)
    {
        if (!speakerEnabled) return;
        int receivedIDLength = BitConverter.ToInt32(receivedMessage.Take(4).ToArray(), 0);
        string receivedID = BitConverter.ToString(receivedMessage.Skip(4).Take(receivedIDLength).ToArray());
        byte[] audioSample = receivedMessage.Skip(4 + receivedIDLength).ToArray();

        if (!waveProviders.ContainsKey(receivedID))
        {
            BufferedWaveProvider waveProvider = new BufferedWaveProvider(waveFormat);
            waveProvider.DiscardOnBufferOverflow = true;
            //waveProvider.BufferDuration
            waveProviders.Add(receivedID, waveProvider);
            mixer.AddMixerInput(waveProvider);
            waveOut.Play();
        }

        waveProviders[receivedID].AddSamples(audioSample, 0, audioSample.Length);
    }

    /// <summary>  
    /// For safety reasons, to avoid "waveProviders" overflow from a large number of different clients  
    /// </summary>  
    private async Task PeriodicallyClearMixer()
    {
        while (true)
        {
            ClearMixerAndWaveProviders();
            //await Task.Delay(TimeSpan.FromMinutes(1));
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    private void ClearMixerAndWaveProviders()
    {
        mixer.RemoveAllMixerInputs();
        lock (waveProviders)
        {
            waveProviders.Clear();
        }
        Debug.Log("Mixer cleared");
    }



    void OnDisable()
    {
        waveIn.Dispose();
        waveOut.Dispose();
    }


}
