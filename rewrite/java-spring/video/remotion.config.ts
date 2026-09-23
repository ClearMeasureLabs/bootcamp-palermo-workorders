import {Config} from 'remotion';

Config.setPublicDir('public');
Config.setOverwriteOutput(true);
Config.setCodec('h264');
Config.setCrf(28);
Config.setPixelFormat('yuv420p');
