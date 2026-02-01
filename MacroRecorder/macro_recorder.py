#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
简单宏录制器 - 最基础的功能
"""

import tkinter as tk
from tkinter import messagebox
import json
import time
import threading
from pynput import mouse, keyboard
from pynput.mouse import Button, Controller as MouseController
from pynput.keyboard import Key, Controller as KeyboardController

class MacroRecorder:
    def __init__(self):
        self.window = tk.Tk()
        self.window.title("宏录制器")
        self.window.geometry("400x300")
        
        self.recording = False
        self.playing = False
        self.actions = []
        self.start_time = None
        
        self.mouse_listener = None
        self.keyboard_listener = None
        self.mouse_controller = MouseController()
        self.keyboard_controller = KeyboardController()
        
        self.create_ui()
        
    def create_ui(self):
        # 状态标签
        self.status_label = tk.Label(self.window, text="准备就绪", font=("Arial", 12))
        self.status_label.pack(pady=10)
        
        # 录制按钮
        self.record_btn = tk.Button(self.window, text="开始录制", command=self.toggle_record, width=15, height=2)
        self.record_btn.pack(pady=10)
        
        # 播放按钮
        self.play_btn = tk.Button(self.window, text="播放", command=self.play, width=15, height=2, state=tk.DISABLED)
        self.play_btn.pack(pady=10)
        
        # 动作数量标签
        self.count_label = tk.Label(self.window, text="动作数量: 0")
        self.count_label.pack(pady=10)
        
    def toggle_record(self):
        if not self.recording:
            self.start_record()
        else:
            self.stop_record()
    
    def start_record(self):
        self.recording = True
        self.actions = []
        self.start_time = time.time()
        
        self.record_btn.config(text="停止录制")
        self.status_label.config(text="正在录制...")
        
        # 启动监听器
        self.mouse_listener = mouse.Listener(
            on_move=self.on_move,
            on_click=self.on_click,
            on_scroll=self.on_scroll
        )
        self.mouse_listener.start()
        
        self.keyboard_listener = keyboard.Listener(
            on_press=self.on_press,
            on_release=self.on_release
        )
        self.keyboard_listener.start()
    
    def stop_record(self):
        self.recording = False
        self.record_btn.config(text="开始录制")
        self.status_label.config(text=f"录制完成 ({len(self.actions)} 个动作)")
        
        if self.mouse_listener:
            self.mouse_listener.stop()
            self.mouse_listener = None
            
        if self.keyboard_listener:
            self.keyboard_listener.stop()
            self.keyboard_listener = None
        
        if self.actions:
            self.play_btn.config(state=tk.NORMAL)
            self.count_label.config(text=f"动作数量: {len(self.actions)}")
    
    def get_timestamp(self):
        return round(time.time() - self.start_time, 4)
    
    def on_move(self, x, y):
        if not self.recording:
            return
        self.actions.append({
            "type": "move",
            "time": self.get_timestamp(),
            "x": int(x),
            "y": int(y)
        })
    
    def on_click(self, x, y, button, pressed):
        if not self.recording:
            return
        self.actions.append({
            "type": "click",
            "time": self.get_timestamp(),
            "x": int(x),
            "y": int(y),
            "button": str(button),
            "pressed": pressed
        })
    
    def on_scroll(self, x, y, dx, dy):
        if not self.recording:
            return
        self.actions.append({
            "type": "scroll",
            "time": self.get_timestamp(),
            "x": int(x),
            "y": int(y),
            "dx": int(dx),
            "dy": int(dy)
        })
    
    def on_press(self, key):
        if not self.recording:
            return
        try:
            key_name = key.char
        except:
            key_name = str(key)
        
        self.actions.append({
            "type": "key_press",
            "time": self.get_timestamp(),
            "key": key_name
        })
    
    def on_release(self, key):
        if not self.recording:
            return
        try:
            key_name = key.char
        except:
            key_name = str(key)
        
        self.actions.append({
            "type": "key_release",
            "time": self.get_timestamp(),
            "key": key_name
        })
    
    def play(self):
        if not self.actions:
            return
        
        self.playing = True
        self.play_btn.config(state=tk.DISABLED)
        self.record_btn.config(state=tk.DISABLED)
        self.status_label.config(text="正在播放...")
        
        # 在新线程中播放
        threading.Thread(target=self._play_actions, daemon=True).start()
    
    def _play_actions(self):
        try:
            start_time = time.time()
            
            for action in self.actions:
                if not self.playing:
                    break
                
                target_time = action["time"]
                elapsed = time.time() - start_time
                wait_time = target_time - elapsed
                
                if wait_time > 0:
                    time.sleep(wait_time)
                
                action_type = action["type"]
                
                if action_type == "move":
                    self.mouse_controller.position = (action["x"], action["y"])
                    
                elif action_type == "click":
                    btn = Button.left if "left" in action["button"] else Button.right
                    if action["pressed"]:
                        self.mouse_controller.press(btn)
                    else:
                        self.mouse_controller.release(btn)
                        
                elif action_type == "scroll":
                    self.mouse_controller.scroll(action["dx"], action["dy"])
                    
                elif action_type == "key_press":
                    key = self.parse_key(action["key"])
                    if key:
                        self.keyboard_controller.press(key)
                        
                elif action_type == "key_release":
                    key = self.parse_key(action["key"])
                    if key:
                        self.keyboard_controller.release(key)
            
            self.playing = False
            self.window.after(0, self.on_playback_complete)
            
        except Exception as e:
            self.playing = False
            self.window.after(0, lambda: messagebox.showerror("错误", str(e)))
    
    def parse_key(self, key_str):
        if not key_str:
            return None
        
        if key_str.startswith("Key."):
            key_name = key_str[4:].lower()
            if hasattr(Key, key_name):
                return getattr(Key, key_name)
        elif len(key_str) == 1:
            return key_str
        
        return None
    
    def on_playback_complete(self):
        self.play_btn.config(state=tk.NORMAL)
        self.record_btn.config(state=tk.NORMAL)
        self.status_label.config(text="播放完成")
    
    def run(self):
        self.window.mainloop()

if __name__ == "__main__":
    app = MacroRecorder()
    app.run()
