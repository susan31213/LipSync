# LipSync
Sync Lip in Unity by Wav2Lip.

## Demo Video
[![LipSync Demo Video](https://drive.google.com/uc?export=view&id=1Y1iQ1PvLV873oMHsXD3nux8S83nNpx-f)](https://youtu.be/THLcyPA4oXw)

## Prerequisites
+ Anaconda (Tested on Windows 10)
+ (if you want to excute AR Mask mode) Unity 2019.4

## Dependencies
+ In Anaconda Prompt, create an environment by:
    ```sh
    conda env create -f Server/wav2lip_conda.yml
    ```

## Usage
### Server
+ After installing Wav2Lip enviornment in Anaconda, activate wav2lip environment in Anaconda Prompt
    ```
    conda activate wav2lip
    ```
+ Start server with a Wav2Lip checkpoint file
    ```sh
    python server.py --checkpoint_path Wav2Lip\checkpoints\wav2lip_gan.pth
    ```
    You can also use different checkpoint in Server/Wav2Lip/checkpoints.
+ Wait for predicting predefine faces.

### Client
+ All of the binary files are built for Win 10.
+ To excute audio only mode, excute `LipSyncUnity/Build/Mask.exe`.
+ To excute AR mask mode, open Unity and play this scene: `Scenes/MaskAR`.
+ Choose the microphone you are using, and click `Start Record` button.
+ Change face by clicking the button on the right hand side.

<img src="https://drive.google.com/uc?export=view&id=1FWAcOsLbD4-TuFIGv4gWW9hZA3eSq4Ph"/>