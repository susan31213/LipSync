# Created by Youssef Elashry to allow two-way communication between Python3 and Unity to send and receive strings

# Feel free to use this in your individual or commercial projects BUT make sure to reference me as: Two-way communication between Python 3 and Unity (C#) - Y. T. Elashry
# It would be appreciated if you send me how you have used this in your projects (e.g. Machine Learning) at youssef.elashry@gmail.com

# Use at your own risk
# Use under the Apache License 2.0

# Example of a Python UDP server

import UdpComms as U
import time
import io
import sys
from Wav2Lip import audio
from Wav2Lip.inference import datagen, load_model, face_detect
from os import listdir, path
import numpy as np
import scipy
import cv2
import os
import sys
import argparse
from enum import Enum
from tqdm import tqdm
import torch
from Wav2Lip.models import Wav2Lip

parser = argparse.ArgumentParser(
    description='Inference code to lip-sync videos in the wild using Wav2Lip models')

parser.add_argument('--checkpoint_path', type=str,
                    help='Name of saved checkpoint to load weights from', required=True)

parser.add_argument('--static', type=bool,
                    help='If True, then use only first video frame for inference', default=False)
parser.add_argument('--fps', type=float, help='Can be specified only if input is a static image (default: 25)',
                    default=5., required=False)

parser.add_argument('--pads', nargs='+', type=int, default=[0, 10, 0, 0],
                    help='Padding (top, bottom, left, right). Please adjust to include chin at least')

parser.add_argument('--face_det_batch_size', type=int,
                    help='Batch size for face detection', default=16)
parser.add_argument('--wav2lip_batch_size', type=int,
                    help='Batch size for Wav2Lip model(s)', default=128)

parser.add_argument('--resize_factor', default=1, type=int,
                    help='Reduce the resolution by this factor. Sometimes, best results are obtained at 480p or 720p')

parser.add_argument('--crop', nargs='+', type=int, default=[0, -1, 0, -1],
                    help='Crop video to a smaller region (top, bottom, left, right). Applied after resize_factor and rotate arg. '
                    'Useful if multiple face present. -1 implies the value will be auto-inferred based on height, width')

parser.add_argument('--box', nargs='+', type=int, default=[-1, -1, -1, -1],
                    help='Specify a constant bounding box for the face. Use only as a last resort if the face is not detected.'
                    'Also, might work only if the face is not moving around much. Syntax: (top, bottom, left, right).')

parser.add_argument('--rotate', default=False, action='store_true',
                    help='Sometimes videos taken from a phone can be flipped 90deg. If true, will flip video right by 90deg.'
                    'Use if you get a flipped result, despite feeding a normal looking video')

parser.add_argument('--nosmooth', default=False, action='store_true',
                    help='Prevent smoothing face detections over a short temporal window')
args = parser.parse_args()
args.img_size = 96

args.static = True

mel_step_size = 16
device = 'cuda' if torch.cuda.is_available() else 'cpu'
print('Using {} for inference.'.format(device))

model = load_model(args.checkpoint_path, device)
print("Model loaded")

people_list = ['Images/trump.png', 'Images/englishtsai.png', 'Images/shi.png', 'Images/suga.png', 'Images/wen.png', 'Images/putin.png', 'Images/lisa.png', 'Images/captain.jpg', 'Images/teacher.png']
face_detects = []
for p in people_list:
    full_frames = [cv2.imread(p)]
    fps = args.fps
    face_det_results = face_detect(full_frames, device, args)
    face_detects.append(face_det_results)
print("face pred finished")

# Create UDP socket to use for sending (and receiving)
sock = U.UdpComms(udpIP="127.0.0.1", portTX=6000, portRX=6001,
                  enableRX=True, suppressWarnings=True)
timer_start = 0
who = 0
img_to_show = np.zeros((args.img_size,args.img_size), dtype=np.int8)

while True:
    data = sock.ReadReceivedData()  # read data

    if data != None:  # if NEW data has been received
        if len(data) == 1:
            who = int.from_bytes(data, byteorder='big')
            continue
        
        timer_start = time.time()
        print('received ' + str(len(data)) + 'bytes', ',who:', who)
        f = io.BytesIO(data)
        wav = audio.load_wav(f, 16000)
        mel = audio.melspectrogram(wav)
        if np.isnan(mel.reshape(-1)).sum() > 0:
            raise ValueError(
                'Mel contains nan! Using a TTS voice? Add a small epsilon noise to the wav file and try again')
        mel_chunks = []
        mel_idx_multiplier = 80./fps
        i = 0
        while 1:
            start_idx = int(i * mel_idx_multiplier)
            if start_idx + mel_step_size > len(mel[0]):
                mel_chunks.append(mel[:, len(mel[0]) - mel_step_size:])
                break
            mel_chunks.append(mel[:, start_idx: start_idx + mel_step_size])
            i += 1

        print("Length of mel chunks: {}".format(len(mel_chunks)))

        full_frames = full_frames[:len(mel_chunks)]

        batch_size = args.wav2lip_batch_size
        gen = datagen(full_frames.copy(), mel_chunks, face_detects[who], device, args)

        for i, (img_batch, mel_batch, frames, coords) in enumerate(tqdm(gen, total=int(np.ceil(float(len(mel_chunks))/batch_size)))):
            img_batch = torch.FloatTensor(
                np.transpose(img_batch, (0, 3, 1, 2))).to(device)
            mel_batch = torch.FloatTensor(
                np.transpose(mel_batch, (0, 3, 1, 2))).to(device)

            with torch.no_grad():
                pred = model(mel_batch, img_batch)

            print('frame shape:', frames[0].shape)

            pred = pred.cpu().numpy().transpose(0, 2, 3, 1) * 255.
            for p, c in zip(pred, coords):
                y1, y2, x1, x2 = c
                p = cv2.resize(p.astype(np.uint8), (x2 - x1, y2 - y1))
                img_to_show = p
                cv2.imshow('image', img_to_show)
                data_to_send = np.array([x1, x2, y1, y2])
                print([x1, x2, y1, y2])
                sock.SendUintData(data_to_send)
                data_to_send = p[:,:,(2,1,0)].flatten()
                print(data_to_send.shape)
                sock.SendUintData(data_to_send)
        print('Used time:', time.time() - timer_start)
        cv2.waitKey(1)
        # time.sleep(0.2)
        
