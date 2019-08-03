import pefile
import argparse
import peutils #https://github.com/erocarrera/pefile
from datetime import datetime # https://linux-notes.org/rabota-s-unix-timestamp-time-na-python/
from langdetect import detect
from langdetect import detect_langs
from pathlib import Path
import string
import os
import csv
from hashlib import sha256

def createParser():
	parser = argparse.ArgumentParser()
	parser.add_argument('-p', '--path', help = 'Path to .exe file', type = str, required = True)
	return parser

def get_time_from_PE(path):
    # Используем функцию из модуля Pefile для анализа PE-заголовка exe. Т.к. нам надо узнать только время компиляции, то
    # отключаем загрузку всех атрибутов с помощью параметра fast_load=True. Это ускорит анализ.
    pe =  pefile.PE(path, fast_load=True)
    # Используем фцнкцию для преобразования TimeStamp в SysTime из модуля Datetime и печатаем ее
    date = datetime.fromtimestamp(pe.FILE_HEADER.TimeDateStamp)
    return date.strftime('%Y-%m-%d %H:%M:%S')
    
def get_file_info(path):
    time = get_time_from_PE(path)
    with open(path, "rb") as f:
        bytes = f.read()
        app_text = bytes.decode("utf-8", "ignore")
        lang = detect(app_text)
        hash = sha256(bytes).hexdigest()
    return time, lang, hash
        
def save_file_data(filename, time, lang, hash):
    currentDir = filename
    fullDir = 'new_dataset/' + currentDir
    os.makedirs(fullDir, exist_ok=True)
    # Сохранение времени компиляции
    f = open(fullDir + '/time.txt', 'w')
    f.write(time)
    f.close()
    # Сохранение языка
    f = open(fullDir + '/lang.txt', 'w')
    f.write(lang)
    f.close()
    # Сохранение хэша sha256
    f = open(fullDir + '/sha256.txt', 'w')
    f.write(hash)
    f.close()

def compare(filename, time, lang, sha256):
    sharedTime = []
    sharedLang = []
    sharedTotal = []

    standartPrograms = []
    featuresArrays = [time, lang]
    featuresFiles = ['time.txt', 'lang.txt']

    # Получаем стандартные программы
    for file in os.listdir('dataset/'):
        standartPrograms.append(file)
    # Задаем критерии сравнения    
    for i in standartPrograms:
        sharedTime.append(0)
        sharedLang.append(0)

    # Сравниваем
    for idp, program in enumerate(standartPrograms):
        for idf, file in enumerate(featuresFiles):
            f = open('dataset/' + program + '/' + file)
            for line in f:
                if line == featuresArrays[idf]:
                    # Сравниваем время
                    if idf == 0:
                        sharedTime[idp] += 1
                    # Сравниваем язык    
                    if idf == 1:
                        sharedLang[idp] += 1
        # Результат сравнения                    
        sharedTotal.append(sharedTime[idp] + sharedLang[idp])

    # Вычисляем наиболее похожую программу
    featuresSize = len(time) + len(lang)
    standartFeaturesSize = 0
    for file in featuresFiles:
        standartFeaturesSize += sum(
            1 for line in open('dataset/' + standartPrograms[sharedTotal.index(max(sharedTotal))] + '/' + file))
    print('The most similar standart .exe or .dll for ' + filename + ' is ' + standartPrograms[sharedTotal.index(max(sharedTotal))]
    + '\nSimularity: ' + str(max(sharedTotal) / standartFeaturesSize))
    # Создаем таблицу
    f = open('dataset/' + program + '/sha256.txt')
    lang_most_simular_hash = f.read()
    with open('out', 'a', newline='') as csvfile:
        writer = csv.writer(csvfile, delimiter=';')
        writer.writerow([sha256, filename, lang_most_simular_hash,  standartPrograms[sharedTotal.index(max(sharedTotal))],
                         str(max(sharedTotal) / standartFeaturesSize)])


def main():
    parser = createParser()
    namespace = parser.parse_args()
    filesList = []
    
    # Создаем таблицу
    with open('out', 'a', newline='') as csvfile:
        writer = csv.writer(csvfile, delimiter=';')
        writer.writerow(['hash', 'filename', 'lang_most_simular_hash',  'lang_most_simular_filename', 'LangSim'])
    
    # Если один файл
    if namespace.path.lower().endswith(('.exe', '.dll')):
        filename = os.path.basename(namespace.path)
        time, lang, sha256 = get_file_info(namespace.path)
        if sha256 != None:
            save_file_data(filename, time, lang, sha256)
            print("time: " + time)
            print("lang: " + lang)
            compare(filename, time, lang, sha256)
    # Если много        
    else:
        for file in os.listdir(namespace.path):
            if file.lower().endswith('.exe') or file.lower().endswith('.dll'):
                filesList.append(os.path.join(namespace.path, file))
        for filepaths in filesList:
            filename = os.path.basename(filepaths)
            time, lang, sha256 = get_file_info(filepaths)
            if sha256 != None:
                save_file_data(filename, time, lang, sha256)
                compare(filename, time, lang, sha256)

if __name__ == '__main__':
	main()
