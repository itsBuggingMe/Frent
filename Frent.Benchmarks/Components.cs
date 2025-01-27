using System.Collections.Immutable;

namespace Frent.Benchmarks;

//compoent sizes roughly follow zipf's law starting from 8 bytes/component
//and incrementing 4 bytes at a time
//1005 unique dummy component types

internal interface IDummyComponent;

internal class BenchmarkedComponentHelper
{
    public static readonly ImmutableArray<string> ComponentNames = [
        "Component8b_0",
        "Component8b_1",
        "Component8b_2",
        "Component8b_3",
        "Component8b_4",
        "Component8b_5",
        "Component8b_6",
        "Component8b_7",
        "Component8b_8",
        "Component8b_9",
        "Component8b_10",
        "Component8b_11",
        "Component8b_12",
        "Component8b_13",
        "Component8b_14",
        "Component8b_15",
        "Component8b_16",
        "Component8b_17",
        "Component8b_18",
        "Component8b_19",
        "Component8b_20",
        "Component8b_21",
        "Component8b_22",
        "Component8b_23",
        "Component8b_24",
        "Component8b_25",
        "Component8b_26",
        "Component8b_27",
        "Component8b_28",
        "Component8b_29",
        "Component8b_30",
        "Component8b_31",
        "Component8b_32",
        "Component8b_33",
        "Component8b_34",
        "Component8b_35",
        "Component8b_36",
        "Component8b_37",
        "Component8b_38",
        "Component8b_39",
        "Component8b_40",
        "Component8b_41",
        "Component8b_42",
        "Component8b_43",
        "Component8b_44",
        "Component8b_45",
        "Component8b_46",
        "Component8b_47",
        "Component8b_48",
        "Component8b_49",
        "Component8b_50",
        "Component8b_51",
        "Component8b_52",
        "Component8b_53",
        "Component8b_54",
        "Component8b_55",
        "Component8b_56",
        "Component8b_57",
        "Component8b_58",
        "Component8b_59",
        "Component8b_60",
        "Component8b_61",
        "Component8b_62",
        "Component8b_63",
        "Component8b_64",
        "Component8b_65",
        "Component8b_66",
        "Component8b_67",
        "Component8b_68",
        "Component8b_69",
        "Component8b_70",
        "Component8b_71",
        "Component8b_72",
        "Component8b_73",
        "Component8b_74",
        "Component8b_75",
        "Component8b_76",
        "Component8b_77",
        "Component8b_78",
        "Component8b_79",
        "Component8b_80",
        "Component8b_81",
        "Component8b_82",
        "Component8b_83",
        "Component8b_84",
        "Component8b_85",
        "Component8b_86",
        "Component8b_87",
        "Component8b_88",
        "Component8b_89",
        "Component8b_90",
        "Component8b_91",
        "Component8b_92",
        "Component8b_93",
        "Component8b_94",
        "Component8b_95",
        "Component8b_96",
        "Component8b_97",
        "Component8b_98",
        "Component8b_99",
        "Component8b_100",
        "Component8b_101",
        "Component8b_102",
        "Component8b_103",
        "Component8b_104",
        "Component8b_105",
        "Component8b_106",
        "Component8b_107",
        "Component8b_108",
        "Component8b_109",
        "Component8b_110",
        "Component8b_111",
        "Component8b_112",
        "Component8b_113",
        "Component8b_114",
        "Component8b_115",
        "Component8b_116",
        "Component8b_117",
        "Component8b_118",
        "Component8b_119",
        "Component8b_120",
        "Component8b_121",
        "Component8b_122",
        "Component8b_123",
        "Component8b_124",
        "Component8b_125",
        "Component8b_126",
        "Component8b_127",
        "Component8b_128",
        "Component8b_129",
        "Component8b_130",
        "Component8b_131",
        "Component8b_132",
        "Component8b_133",
        "Component8b_134",
        "Component8b_135",
        "Component8b_136",
        "Component8b_137",
        "Component8b_138",
        "Component8b_139",
        "Component8b_140",
        "Component8b_141",
        "Component8b_142",
        "Component8b_143",
        "Component8b_144",
        "Component8b_145",
        "Component8b_146",
        "Component8b_147",
        "Component8b_148",
        "Component8b_149",
        "Component8b_150",
        "Component8b_151",
        "Component8b_152",
        "Component8b_153",
        "Component8b_154",
        "Component8b_155",
        "Component8b_156",
        "Component8b_157",
        "Component8b_158",
        "Component8b_159",
        "Component8b_160",
        "Component8b_161",
        "Component8b_162",
        "Component8b_163",
        "Component8b_164",
        "Component8b_165",
        "Component8b_166",
        "Component8b_167",
        "Component8b_168",
        "Component8b_169",
        "Component8b_170",
        "Component8b_171",
        "Component8b_172",
        "Component8b_173",
        "Component8b_174",
        "Component8b_175",
        "Component8b_176",
        "Component8b_177",
        "Component8b_178",
        "Component8b_179",
        "Component8b_180",
        "Component8b_181",
        "Component8b_182",
        "Component8b_183",
        "Component8b_184",
        "Component8b_185",
        "Component8b_186",
        "Component8b_187",
        "Component8b_188",
        "Component8b_189",
        "Component8b_190",
        "Component8b_191",
        "Component8b_192",
        "Component8b_193",
        "Component8b_194",
        "Component8b_195",
        "Component8b_196",
        "Component8b_197",
        "Component8b_198",
        "Component8b_199",
        "Component8b_200",
        "Component8b_201",
        "Component8b_202",
        "Component8b_203",
        "Component8b_204",
        "Component8b_205",
        "Component8b_206",
        "Component8b_207",
        "Component8b_208",
        "Component8b_209",
        "Component8b_210",
        "Component8b_211",
        "Component8b_212",
        "Component8b_213",
        "Component8b_214",
        "Component8b_215",
        "Component8b_216",
        "Component8b_217",
        "Component8b_218",
        "Component8b_219",
        "Component8b_220",
        "Component8b_221",
        "Component8b_222",
        "Component8b_223",
        "Component8b_224",
        "Component8b_225",
        "Component8b_226",
        "Component8b_227",
        "Component8b_228",
        "Component8b_229",
        "Component8b_230",
        "Component8b_231",
        "Component8b_232",
        "Component8b_233",
        "Component8b_234",
        "Component8b_235",
        "Component8b_236",
        "Component8b_237",
        "Component8b_238",
        "Component8b_239",
        "Component8b_240",
        "Component8b_241",
        "Component8b_242",
        "Component8b_243",
        "Component8b_244",
        "Component8b_245",
        "Component8b_246",
        "Component8b_247",
        "Component8b_248",
        "Component8b_249",
        "Component8b_250",
        "Component8b_251",
        "Component12b_0",
        "Component12b_1",
        "Component12b_2",
        "Component12b_3",
        "Component12b_4",
        "Component12b_5",
        "Component12b_6",
        "Component12b_7",
        "Component12b_8",
        "Component12b_9",
        "Component12b_10",
        "Component12b_11",
        "Component12b_12",
        "Component12b_13",
        "Component12b_14",
        "Component12b_15",
        "Component12b_16",
        "Component12b_17",
        "Component12b_18",
        "Component12b_19",
        "Component12b_20",
        "Component12b_21",
        "Component12b_22",
        "Component12b_23",
        "Component12b_24",
        "Component12b_25",
        "Component12b_26",
        "Component12b_27",
        "Component12b_28",
        "Component12b_29",
        "Component12b_30",
        "Component12b_31",
        "Component12b_32",
        "Component12b_33",
        "Component12b_34",
        "Component12b_35",
        "Component12b_36",
        "Component12b_37",
        "Component12b_38",
        "Component12b_39",
        "Component12b_40",
        "Component12b_41",
        "Component12b_42",
        "Component12b_43",
        "Component12b_44",
        "Component12b_45",
        "Component12b_46",
        "Component12b_47",
        "Component12b_48",
        "Component12b_49",
        "Component12b_50",
        "Component12b_51",
        "Component12b_52",
        "Component12b_53",
        "Component12b_54",
        "Component12b_55",
        "Component12b_56",
        "Component12b_57",
        "Component12b_58",
        "Component12b_59",
        "Component12b_60",
        "Component12b_61",
        "Component12b_62",
        "Component12b_63",
        "Component12b_64",
        "Component12b_65",
        "Component12b_66",
        "Component12b_67",
        "Component12b_68",
        "Component12b_69",
        "Component12b_70",
        "Component12b_71",
        "Component12b_72",
        "Component12b_73",
        "Component12b_74",
        "Component12b_75",
        "Component12b_76",
        "Component12b_77",
        "Component12b_78",
        "Component12b_79",
        "Component12b_80",
        "Component12b_81",
        "Component12b_82",
        "Component12b_83",
        "Component12b_84",
        "Component12b_85",
        "Component12b_86",
        "Component12b_87",
        "Component12b_88",
        "Component12b_89",
        "Component12b_90",
        "Component12b_91",
        "Component12b_92",
        "Component12b_93",
        "Component12b_94",
        "Component12b_95",
        "Component12b_96",
        "Component12b_97",
        "Component12b_98",
        "Component12b_99",
        "Component12b_100",
        "Component12b_101",
        "Component12b_102",
        "Component12b_103",
        "Component12b_104",
        "Component12b_105",
        "Component12b_106",
        "Component12b_107",
        "Component12b_108",
        "Component12b_109",
        "Component12b_110",
        "Component12b_111",
        "Component12b_112",
        "Component12b_113",
        "Component12b_114",
        "Component12b_115",
        "Component12b_116",
        "Component12b_117",
        "Component12b_118",
        "Component12b_119",
        "Component12b_120",
        "Component12b_121",
        "Component12b_122",
        "Component12b_123",
        "Component12b_124",
        "Component12b_125",
        "Component16b_0",
        "Component16b_1",
        "Component16b_2",
        "Component16b_3",
        "Component16b_4",
        "Component16b_5",
        "Component16b_6",
        "Component16b_7",
        "Component16b_8",
        "Component16b_9",
        "Component16b_10",
        "Component16b_11",
        "Component16b_12",
        "Component16b_13",
        "Component16b_14",
        "Component16b_15",
        "Component16b_16",
        "Component16b_17",
        "Component16b_18",
        "Component16b_19",
        "Component16b_20",
        "Component16b_21",
        "Component16b_22",
        "Component16b_23",
        "Component16b_24",
        "Component16b_25",
        "Component16b_26",
        "Component16b_27",
        "Component16b_28",
        "Component16b_29",
        "Component16b_30",
        "Component16b_31",
        "Component16b_32",
        "Component16b_33",
        "Component16b_34",
        "Component16b_35",
        "Component16b_36",
        "Component16b_37",
        "Component16b_38",
        "Component16b_39",
        "Component16b_40",
        "Component16b_41",
        "Component16b_42",
        "Component16b_43",
        "Component16b_44",
        "Component16b_45",
        "Component16b_46",
        "Component16b_47",
        "Component16b_48",
        "Component16b_49",
        "Component16b_50",
        "Component16b_51",
        "Component16b_52",
        "Component16b_53",
        "Component16b_54",
        "Component16b_55",
        "Component16b_56",
        "Component16b_57",
        "Component16b_58",
        "Component16b_59",
        "Component16b_60",
        "Component16b_61",
        "Component16b_62",
        "Component16b_63",
        "Component16b_64",
        "Component16b_65",
        "Component16b_66",
        "Component16b_67",
        "Component16b_68",
        "Component16b_69",
        "Component16b_70",
        "Component16b_71",
        "Component16b_72",
        "Component16b_73",
        "Component16b_74",
        "Component16b_75",
        "Component16b_76",
        "Component16b_77",
        "Component16b_78",
        "Component16b_79",
        "Component16b_80",
        "Component16b_81",
        "Component16b_82",
        "Component16b_83",
        "Component20b_0",
        "Component20b_1",
        "Component20b_2",
        "Component20b_3",
        "Component20b_4",
        "Component20b_5",
        "Component20b_6",
        "Component20b_7",
        "Component20b_8",
        "Component20b_9",
        "Component20b_10",
        "Component20b_11",
        "Component20b_12",
        "Component20b_13",
        "Component20b_14",
        "Component20b_15",
        "Component20b_16",
        "Component20b_17",
        "Component20b_18",
        "Component20b_19",
        "Component20b_20",
        "Component20b_21",
        "Component20b_22",
        "Component20b_23",
        "Component20b_24",
        "Component20b_25",
        "Component20b_26",
        "Component20b_27",
        "Component20b_28",
        "Component20b_29",
        "Component20b_30",
        "Component20b_31",
        "Component20b_32",
        "Component20b_33",
        "Component20b_34",
        "Component20b_35",
        "Component20b_36",
        "Component20b_37",
        "Component20b_38",
        "Component20b_39",
        "Component20b_40",
        "Component20b_41",
        "Component20b_42",
        "Component20b_43",
        "Component20b_44",
        "Component20b_45",
        "Component20b_46",
        "Component20b_47",
        "Component20b_48",
        "Component20b_49",
        "Component20b_50",
        "Component20b_51",
        "Component20b_52",
        "Component20b_53",
        "Component20b_54",
        "Component20b_55",
        "Component20b_56",
        "Component20b_57",
        "Component20b_58",
        "Component20b_59",
        "Component20b_60",
        "Component20b_61",
        "Component20b_62",
        "Component24b_0",
        "Component24b_1",
        "Component24b_2",
        "Component24b_3",
        "Component24b_4",
        "Component24b_5",
        "Component24b_6",
        "Component24b_7",
        "Component24b_8",
        "Component24b_9",
        "Component24b_10",
        "Component24b_11",
        "Component24b_12",
        "Component24b_13",
        "Component24b_14",
        "Component24b_15",
        "Component24b_16",
        "Component24b_17",
        "Component24b_18",
        "Component24b_19",
        "Component24b_20",
        "Component24b_21",
        "Component24b_22",
        "Component24b_23",
        "Component24b_24",
        "Component24b_25",
        "Component24b_26",
        "Component24b_27",
        "Component24b_28",
        "Component24b_29",
        "Component24b_30",
        "Component24b_31",
        "Component24b_32",
        "Component24b_33",
        "Component24b_34",
        "Component24b_35",
        "Component24b_36",
        "Component24b_37",
        "Component24b_38",
        "Component24b_39",
        "Component24b_40",
        "Component24b_41",
        "Component24b_42",
        "Component24b_43",
        "Component24b_44",
        "Component24b_45",
        "Component24b_46",
        "Component24b_47",
        "Component24b_48",
        "Component24b_49",
        "Component28b_0",
        "Component28b_1",
        "Component28b_2",
        "Component28b_3",
        "Component28b_4",
        "Component28b_5",
        "Component28b_6",
        "Component28b_7",
        "Component28b_8",
        "Component28b_9",
        "Component28b_10",
        "Component28b_11",
        "Component28b_12",
        "Component28b_13",
        "Component28b_14",
        "Component28b_15",
        "Component28b_16",
        "Component28b_17",
        "Component28b_18",
        "Component28b_19",
        "Component28b_20",
        "Component28b_21",
        "Component28b_22",
        "Component28b_23",
        "Component28b_24",
        "Component28b_25",
        "Component28b_26",
        "Component28b_27",
        "Component28b_28",
        "Component28b_29",
        "Component28b_30",
        "Component28b_31",
        "Component28b_32",
        "Component28b_33",
        "Component28b_34",
        "Component28b_35",
        "Component28b_36",
        "Component28b_37",
        "Component28b_38",
        "Component28b_39",
        "Component28b_40",
        "Component28b_41",
        "Component32b_0",
        "Component32b_1",
        "Component32b_2",
        "Component32b_3",
        "Component32b_4",
        "Component32b_5",
        "Component32b_6",
        "Component32b_7",
        "Component32b_8",
        "Component32b_9",
        "Component32b_10",
        "Component32b_11",
        "Component32b_12",
        "Component32b_13",
        "Component32b_14",
        "Component32b_15",
        "Component32b_16",
        "Component32b_17",
        "Component32b_18",
        "Component32b_19",
        "Component32b_20",
        "Component32b_21",
        "Component32b_22",
        "Component32b_23",
        "Component32b_24",
        "Component32b_25",
        "Component32b_26",
        "Component32b_27",
        "Component32b_28",
        "Component32b_29",
        "Component32b_30",
        "Component32b_31",
        "Component32b_32",
        "Component32b_33",
        "Component32b_34",
        "Component32b_35",
        "Component36b_0",
        "Component36b_1",
        "Component36b_2",
        "Component36b_3",
        "Component36b_4",
        "Component36b_5",
        "Component36b_6",
        "Component36b_7",
        "Component36b_8",
        "Component36b_9",
        "Component36b_10",
        "Component36b_11",
        "Component36b_12",
        "Component36b_13",
        "Component36b_14",
        "Component36b_15",
        "Component36b_16",
        "Component36b_17",
        "Component36b_18",
        "Component36b_19",
        "Component36b_20",
        "Component36b_21",
        "Component36b_22",
        "Component36b_23",
        "Component36b_24",
        "Component36b_25",
        "Component36b_26",
        "Component36b_27",
        "Component36b_28",
        "Component36b_29",
        "Component36b_30",
        "Component40b_0",
        "Component40b_1",
        "Component40b_2",
        "Component40b_3",
        "Component40b_4",
        "Component40b_5",
        "Component40b_6",
        "Component40b_7",
        "Component40b_8",
        "Component40b_9",
        "Component40b_10",
        "Component40b_11",
        "Component40b_12",
        "Component40b_13",
        "Component40b_14",
        "Component40b_15",
        "Component40b_16",
        "Component40b_17",
        "Component40b_18",
        "Component40b_19",
        "Component40b_20",
        "Component40b_21",
        "Component40b_22",
        "Component40b_23",
        "Component40b_24",
        "Component40b_25",
        "Component40b_26",
        "Component40b_27",
        "Component44b_0",
        "Component44b_1",
        "Component44b_2",
        "Component44b_3",
        "Component44b_4",
        "Component44b_5",
        "Component44b_6",
        "Component44b_7",
        "Component44b_8",
        "Component44b_9",
        "Component44b_10",
        "Component44b_11",
        "Component44b_12",
        "Component44b_13",
        "Component44b_14",
        "Component44b_15",
        "Component44b_16",
        "Component44b_17",
        "Component44b_18",
        "Component44b_19",
        "Component44b_20",
        "Component44b_21",
        "Component44b_22",
        "Component44b_23",
        "Component44b_24",
        "Component48b_0",
        "Component48b_1",
        "Component48b_2",
        "Component48b_3",
        "Component48b_4",
        "Component48b_5",
        "Component48b_6",
        "Component48b_7",
        "Component48b_8",
        "Component48b_9",
        "Component48b_10",
        "Component48b_11",
        "Component48b_12",
        "Component48b_13",
        "Component48b_14",
        "Component48b_15",
        "Component48b_16",
        "Component48b_17",
        "Component48b_18",
        "Component48b_19",
        "Component48b_20",
        "Component48b_21",
        "Component52b_0",
        "Component52b_1",
        "Component52b_2",
        "Component52b_3",
        "Component52b_4",
        "Component52b_5",
        "Component52b_6",
        "Component52b_7",
        "Component52b_8",
        "Component52b_9",
        "Component52b_10",
        "Component52b_11",
        "Component52b_12",
        "Component52b_13",
        "Component52b_14",
        "Component52b_15",
        "Component52b_16",
        "Component52b_17",
        "Component52b_18",
        "Component52b_19",
        "Component52b_20",
        "Component56b_0",
        "Component56b_1",
        "Component56b_2",
        "Component56b_3",
        "Component56b_4",
        "Component56b_5",
        "Component56b_6",
        "Component56b_7",
        "Component56b_8",
        "Component56b_9",
        "Component56b_10",
        "Component56b_11",
        "Component56b_12",
        "Component56b_13",
        "Component56b_14",
        "Component56b_15",
        "Component56b_16",
        "Component56b_17",
        "Component56b_18",
        "Component60b_0",
        "Component60b_1",
        "Component60b_2",
        "Component60b_3",
        "Component60b_4",
        "Component60b_5",
        "Component60b_6",
        "Component60b_7",
        "Component60b_8",
        "Component60b_9",
        "Component60b_10",
        "Component60b_11",
        "Component60b_12",
        "Component60b_13",
        "Component60b_14",
        "Component60b_15",
        "Component60b_16",
        "Component60b_17",
        "Component64b_0",
        "Component64b_1",
        "Component64b_2",
        "Component64b_3",
        "Component64b_4",
        "Component64b_5",
        "Component64b_6",
        "Component64b_7",
        "Component64b_8",
        "Component64b_9",
        "Component64b_10",
        "Component64b_11",
        "Component64b_12",
        "Component64b_13",
        "Component64b_14",
        "Component64b_15",
        "Component68b_0",
        "Component68b_1",
        "Component68b_2",
        "Component68b_3",
        "Component68b_4",
        "Component68b_5",
        "Component68b_6",
        "Component68b_7",
        "Component68b_8",
        "Component68b_9",
        "Component68b_10",
        "Component68b_11",
        "Component68b_12",
        "Component68b_13",
        "Component68b_14",
        "Component72b_0",
        "Component72b_1",
        "Component72b_2",
        "Component72b_3",
        "Component72b_4",
        "Component72b_5",
        "Component72b_6",
        "Component72b_7",
        "Component72b_8",
        "Component72b_9",
        "Component72b_10",
        "Component72b_11",
        "Component72b_12",
        "Component72b_13",
        "Component76b_0",
        "Component76b_1",
        "Component76b_2",
        "Component76b_3",
        "Component76b_4",
        "Component76b_5",
        "Component76b_6",
        "Component76b_7",
        "Component76b_8",
        "Component76b_9",
        "Component76b_10",
        "Component76b_11",
        "Component76b_12",
        "Component76b_13",
        "Component80b_0",
        "Component80b_1",
        "Component80b_2",
        "Component80b_3",
        "Component80b_4",
        "Component80b_5",
        "Component80b_6",
        "Component80b_7",
        "Component80b_8",
        "Component80b_9",
        "Component80b_10",
        "Component80b_11",
        "Component80b_12",
        "Component84b_0",
        "Component84b_1",
        "Component84b_2",
        "Component84b_3",
        "Component84b_4",
        "Component84b_5",
        "Component84b_6",
        "Component84b_7",
        "Component84b_8",
        "Component84b_9",
        "Component84b_10",
        "Component84b_11",
        "Component88b_0",
        "Component88b_1",
        "Component88b_2",
        "Component88b_3",
        "Component88b_4",
        "Component88b_5",
        "Component88b_6",
        "Component88b_7",
        "Component88b_8",
        "Component88b_9",
        "Component88b_10",
        "Component88b_11",
        "Component92b_0",
        "Component92b_1",
        "Component92b_2",
        "Component92b_3",
        "Component92b_4",
        "Component92b_5",
        "Component92b_6",
        "Component92b_7",
        "Component92b_8",
        "Component92b_9",
        "Component92b_10",
        "Component96b_0",
        "Component96b_1",
        "Component96b_2",
        "Component96b_3",
        "Component96b_4",
        "Component96b_5",
        "Component96b_6",
        "Component96b_7",
        "Component96b_8",
        "Component96b_9",
        "Component100b_0",
        "Component100b_1",
        "Component100b_2",
        "Component100b_3",
        "Component100b_4",
        "Component100b_5",
        "Component100b_6",
        "Component100b_7",
        "Component100b_8",
        "Component100b_9",
        "Component104b_0",
        "Component104b_1",
        "Component104b_2",
        "Component104b_3",
        "Component104b_4",
        "Component104b_5",
        "Component104b_6",
        "Component104b_7",
        "Component104b_8",
        "Component104b_9",
        "Component108b_0",
        "Component108b_1",
        "Component108b_2",
        "Component108b_3",
        "Component108b_4",
        "Component108b_5",
        "Component108b_6",
        "Component108b_7",
        "Component108b_8",
        "Component112b_0",
        "Component112b_1",
        "Component112b_2",
        "Component112b_3",
        "Component112b_4",
        "Component112b_5",
        "Component112b_6",
        "Component112b_7",
        "Component112b_8",
        "Component116b_0",
        "Component116b_1",
        "Component116b_2",
        "Component116b_3",
        "Component116b_4",
        "Component116b_5",
        "Component116b_6",
        "Component116b_7",
        "Component116b_8",
        "Component120b_0",
        "Component120b_1",
        "Component120b_2",
        "Component120b_3",
        "Component120b_4",
        "Component120b_5",
        "Component120b_6",
        "Component120b_7",
        "Component124b_0",
        "Component124b_1",
        "Component124b_2",
        "Component124b_3",
        "Component124b_4",
        "Component124b_5",
        "Component124b_6",
        "Component124b_7",
        "Component128b_0",
        "Component128b_1",
        "Component128b_2",
        "Component128b_3",
        "Component128b_4",
        "Component128b_5",
        "Component128b_6",
        "Component128b_7",
    ];
}

record struct Component8b_0(int f0, int f4) : IDummyComponent;
record struct Component8b_1(float f0, int f4) : IDummyComponent;
record struct Component8b_2(int f0, float f4) : IDummyComponent;
record struct Component8b_3(float f0, float f4) : IDummyComponent;
record struct Component8b_4(float f0, int f4) : IDummyComponent;
record struct Component8b_5(float f0, int f4) : IDummyComponent;
record struct Component8b_6(float f0, float f4) : IDummyComponent;
record struct Component8b_7(int f0, float f4) : IDummyComponent;
record struct Component8b_8(int f0, int f4) : IDummyComponent;
record struct Component8b_9(float f0, int f4) : IDummyComponent;
record struct Component8b_10(int f0, float f4) : IDummyComponent;
record struct Component8b_11(float f0, int f4) : IDummyComponent;
record struct Component8b_12(int f0, float f4) : IDummyComponent;
record struct Component8b_13(int f0, float f4) : IDummyComponent;
record struct Component8b_14(float f0, float f4) : IDummyComponent;
record struct Component8b_15(float f0, float f4) : IDummyComponent;
record struct Component8b_16(float f0, float f4) : IDummyComponent;
record struct Component8b_17(int f0, float f4) : IDummyComponent;
record struct Component8b_18(float f0, float f4) : IDummyComponent;
record struct Component8b_19(int f0, int f4) : IDummyComponent;
record struct Component8b_20(float f0, int f4) : IDummyComponent;
record struct Component8b_21(int f0, float f4) : IDummyComponent;
record struct Component8b_22(int f0, float f4) : IDummyComponent;
record struct Component8b_23(int f0, float f4) : IDummyComponent;
record struct Component8b_24(float f0, int f4) : IDummyComponent;
record struct Component8b_25(float f0, float f4) : IDummyComponent;
record struct Component8b_26(float f0, float f4) : IDummyComponent;
record struct Component8b_27(float f0, int f4) : IDummyComponent;
record struct Component8b_28(int f0, int f4) : IDummyComponent;
record struct Component8b_29(int f0, float f4) : IDummyComponent;
record struct Component8b_30(float f0, int f4) : IDummyComponent;
record struct Component8b_31(int f0, int f4) : IDummyComponent;
record struct Component8b_32(int f0, int f4) : IDummyComponent;
record struct Component8b_33(float f0, float f4) : IDummyComponent;
record struct Component8b_34(int f0, float f4) : IDummyComponent;
record struct Component8b_35(float f0, int f4) : IDummyComponent;
record struct Component8b_36(float f0, int f4) : IDummyComponent;
record struct Component8b_37(float f0, float f4) : IDummyComponent;
record struct Component8b_38(int f0, float f4) : IDummyComponent;
record struct Component8b_39(int f0, int f4) : IDummyComponent;
record struct Component8b_40(int f0, float f4) : IDummyComponent;
record struct Component8b_41(int f0, int f4) : IDummyComponent;
record struct Component8b_42(float f0, int f4) : IDummyComponent;
record struct Component8b_43(int f0, float f4) : IDummyComponent;
record struct Component8b_44(int f0, int f4) : IDummyComponent;
record struct Component8b_45(int f0, float f4) : IDummyComponent;
record struct Component8b_46(int f0, float f4) : IDummyComponent;
record struct Component8b_47(float f0, int f4) : IDummyComponent;
record struct Component8b_48(int f0, int f4) : IDummyComponent;
record struct Component8b_49(float f0, float f4) : IDummyComponent;
record struct Component8b_50(int f0, int f4) : IDummyComponent;
record struct Component8b_51(float f0, int f4) : IDummyComponent;
record struct Component8b_52(int f0, int f4) : IDummyComponent;
record struct Component8b_53(float f0, float f4) : IDummyComponent;
record struct Component8b_54(float f0, float f4) : IDummyComponent;
record struct Component8b_55(int f0, float f4) : IDummyComponent;
record struct Component8b_56(int f0, float f4) : IDummyComponent;
record struct Component8b_57(int f0, float f4) : IDummyComponent;
record struct Component8b_58(float f0, float f4) : IDummyComponent;
record struct Component8b_59(float f0, float f4) : IDummyComponent;
record struct Component8b_60(float f0, float f4) : IDummyComponent;
record struct Component8b_61(int f0, float f4) : IDummyComponent;
record struct Component8b_62(int f0, int f4) : IDummyComponent;
record struct Component8b_63(float f0, int f4) : IDummyComponent;
record struct Component8b_64(int f0, int f4) : IDummyComponent;
record struct Component8b_65(float f0, int f4) : IDummyComponent;
record struct Component8b_66(float f0, int f4) : IDummyComponent;
record struct Component8b_67(float f0, float f4) : IDummyComponent;
record struct Component8b_68(int f0, float f4) : IDummyComponent;
record struct Component8b_69(int f0, float f4) : IDummyComponent;
record struct Component8b_70(int f0, int f4) : IDummyComponent;
record struct Component8b_71(int f0, float f4) : IDummyComponent;
record struct Component8b_72(float f0, int f4) : IDummyComponent;
record struct Component8b_73(float f0, float f4) : IDummyComponent;
record struct Component8b_74(float f0, int f4) : IDummyComponent;
record struct Component8b_75(int f0, float f4) : IDummyComponent;
record struct Component8b_76(int f0, int f4) : IDummyComponent;
record struct Component8b_77(int f0, int f4) : IDummyComponent;
record struct Component8b_78(int f0, float f4) : IDummyComponent;
record struct Component8b_79(int f0, int f4) : IDummyComponent;
record struct Component8b_80(float f0, float f4) : IDummyComponent;
record struct Component8b_81(float f0, float f4) : IDummyComponent;
record struct Component8b_82(int f0, float f4) : IDummyComponent;
record struct Component8b_83(int f0, float f4) : IDummyComponent;
record struct Component8b_84(float f0, float f4) : IDummyComponent;
record struct Component8b_85(float f0, float f4) : IDummyComponent;
record struct Component8b_86(int f0, float f4) : IDummyComponent;
record struct Component8b_87(int f0, int f4) : IDummyComponent;
record struct Component8b_88(float f0, int f4) : IDummyComponent;
record struct Component8b_89(int f0, int f4) : IDummyComponent;
record struct Component8b_90(float f0, float f4) : IDummyComponent;
record struct Component8b_91(float f0, int f4) : IDummyComponent;
record struct Component8b_92(int f0, float f4) : IDummyComponent;
record struct Component8b_93(float f0, float f4) : IDummyComponent;
record struct Component8b_94(int f0, int f4) : IDummyComponent;
record struct Component8b_95(float f0, int f4) : IDummyComponent;
record struct Component8b_96(float f0, float f4) : IDummyComponent;
record struct Component8b_97(float f0, int f4) : IDummyComponent;
record struct Component8b_98(int f0, int f4) : IDummyComponent;
record struct Component8b_99(int f0, float f4) : IDummyComponent;
record struct Component8b_100(float f0, float f4) : IDummyComponent;
record struct Component8b_101(int f0, float f4) : IDummyComponent;
record struct Component8b_102(float f0, float f4) : IDummyComponent;
record struct Component8b_103(int f0, float f4) : IDummyComponent;
record struct Component8b_104(float f0, float f4) : IDummyComponent;
record struct Component8b_105(float f0, int f4) : IDummyComponent;
record struct Component8b_106(float f0, int f4) : IDummyComponent;
record struct Component8b_107(int f0, float f4) : IDummyComponent;
record struct Component8b_108(float f0, int f4) : IDummyComponent;
record struct Component8b_109(float f0, int f4) : IDummyComponent;
record struct Component8b_110(float f0, int f4) : IDummyComponent;
record struct Component8b_111(int f0, int f4) : IDummyComponent;
record struct Component8b_112(float f0, float f4) : IDummyComponent;
record struct Component8b_113(float f0, int f4) : IDummyComponent;
record struct Component8b_114(float f0, int f4) : IDummyComponent;
record struct Component8b_115(int f0, int f4) : IDummyComponent;
record struct Component8b_116(int f0, float f4) : IDummyComponent;
record struct Component8b_117(float f0, float f4) : IDummyComponent;
record struct Component8b_118(int f0, int f4) : IDummyComponent;
record struct Component8b_119(float f0, int f4) : IDummyComponent;
record struct Component8b_120(int f0, int f4) : IDummyComponent;
record struct Component8b_121(int f0, int f4) : IDummyComponent;
record struct Component8b_122(float f0, float f4) : IDummyComponent;
record struct Component8b_123(float f0, int f4) : IDummyComponent;
record struct Component8b_124(int f0, float f4) : IDummyComponent;
record struct Component8b_125(int f0, int f4) : IDummyComponent;
record struct Component8b_126(float f0, float f4) : IDummyComponent;
record struct Component8b_127(float f0, float f4) : IDummyComponent;
record struct Component8b_128(float f0, float f4) : IDummyComponent;
record struct Component8b_129(float f0, int f4) : IDummyComponent;
record struct Component8b_130(int f0, float f4) : IDummyComponent;
record struct Component8b_131(int f0, float f4) : IDummyComponent;
record struct Component8b_132(float f0, float f4) : IDummyComponent;
record struct Component8b_133(int f0, float f4) : IDummyComponent;
record struct Component8b_134(float f0, int f4) : IDummyComponent;
record struct Component8b_135(float f0, float f4) : IDummyComponent;
record struct Component8b_136(int f0, float f4) : IDummyComponent;
record struct Component8b_137(float f0, int f4) : IDummyComponent;
record struct Component8b_138(float f0, float f4) : IDummyComponent;
record struct Component8b_139(float f0, float f4) : IDummyComponent;
record struct Component8b_140(float f0, float f4) : IDummyComponent;
record struct Component8b_141(int f0, int f4) : IDummyComponent;
record struct Component8b_142(float f0, int f4) : IDummyComponent;
record struct Component8b_143(int f0, int f4) : IDummyComponent;
record struct Component8b_144(float f0, float f4) : IDummyComponent;
record struct Component8b_145(int f0, float f4) : IDummyComponent;
record struct Component8b_146(float f0, int f4) : IDummyComponent;
record struct Component8b_147(int f0, int f4) : IDummyComponent;
record struct Component8b_148(int f0, float f4) : IDummyComponent;
record struct Component8b_149(float f0, float f4) : IDummyComponent;
record struct Component8b_150(int f0, int f4) : IDummyComponent;
record struct Component8b_151(int f0, int f4) : IDummyComponent;
record struct Component8b_152(int f0, float f4) : IDummyComponent;
record struct Component8b_153(int f0, float f4) : IDummyComponent;
record struct Component8b_154(float f0, int f4) : IDummyComponent;
record struct Component8b_155(int f0, int f4) : IDummyComponent;
record struct Component8b_156(float f0, float f4) : IDummyComponent;
record struct Component8b_157(float f0, float f4) : IDummyComponent;
record struct Component8b_158(float f0, int f4) : IDummyComponent;
record struct Component8b_159(int f0, float f4) : IDummyComponent;
record struct Component8b_160(float f0, int f4) : IDummyComponent;
record struct Component8b_161(int f0, int f4) : IDummyComponent;
record struct Component8b_162(float f0, int f4) : IDummyComponent;
record struct Component8b_163(float f0, int f4) : IDummyComponent;
record struct Component8b_164(float f0, int f4) : IDummyComponent;
record struct Component8b_165(float f0, float f4) : IDummyComponent;
record struct Component8b_166(float f0, float f4) : IDummyComponent;
record struct Component8b_167(int f0, int f4) : IDummyComponent;
record struct Component8b_168(float f0, int f4) : IDummyComponent;
record struct Component8b_169(int f0, int f4) : IDummyComponent;
record struct Component8b_170(float f0, int f4) : IDummyComponent;
record struct Component8b_171(float f0, int f4) : IDummyComponent;
record struct Component8b_172(float f0, int f4) : IDummyComponent;
record struct Component8b_173(int f0, int f4) : IDummyComponent;
record struct Component8b_174(int f0, int f4) : IDummyComponent;
record struct Component8b_175(float f0, float f4) : IDummyComponent;
record struct Component8b_176(int f0, int f4) : IDummyComponent;
record struct Component8b_177(float f0, int f4) : IDummyComponent;
record struct Component8b_178(int f0, int f4) : IDummyComponent;
record struct Component8b_179(float f0, float f4) : IDummyComponent;
record struct Component8b_180(float f0, float f4) : IDummyComponent;
record struct Component8b_181(float f0, float f4) : IDummyComponent;
record struct Component8b_182(float f0, float f4) : IDummyComponent;
record struct Component8b_183(int f0, float f4) : IDummyComponent;
record struct Component8b_184(int f0, int f4) : IDummyComponent;
record struct Component8b_185(int f0, float f4) : IDummyComponent;
record struct Component8b_186(int f0, float f4) : IDummyComponent;
record struct Component8b_187(float f0, float f4) : IDummyComponent;
record struct Component8b_188(int f0, int f4) : IDummyComponent;
record struct Component8b_189(int f0, int f4) : IDummyComponent;
record struct Component8b_190(int f0, int f4) : IDummyComponent;
record struct Component8b_191(float f0, float f4) : IDummyComponent;
record struct Component8b_192(int f0, float f4) : IDummyComponent;
record struct Component8b_193(float f0, int f4) : IDummyComponent;
record struct Component8b_194(int f0, int f4) : IDummyComponent;
record struct Component8b_195(int f0, int f4) : IDummyComponent;
record struct Component8b_196(int f0, int f4) : IDummyComponent;
record struct Component8b_197(float f0, float f4) : IDummyComponent;
record struct Component8b_198(float f0, int f4) : IDummyComponent;
record struct Component8b_199(float f0, int f4) : IDummyComponent;
record struct Component8b_200(float f0, int f4) : IDummyComponent;
record struct Component8b_201(float f0, int f4) : IDummyComponent;
record struct Component8b_202(int f0, float f4) : IDummyComponent;
record struct Component8b_203(int f0, int f4) : IDummyComponent;
record struct Component8b_204(int f0, float f4) : IDummyComponent;
record struct Component8b_205(float f0, float f4) : IDummyComponent;
record struct Component8b_206(float f0, int f4) : IDummyComponent;
record struct Component8b_207(int f0, int f4) : IDummyComponent;
record struct Component8b_208(float f0, float f4) : IDummyComponent;
record struct Component8b_209(int f0, int f4) : IDummyComponent;
record struct Component8b_210(float f0, int f4) : IDummyComponent;
record struct Component8b_211(int f0, int f4) : IDummyComponent;
record struct Component8b_212(int f0, int f4) : IDummyComponent;
record struct Component8b_213(int f0, int f4) : IDummyComponent;
record struct Component8b_214(int f0, float f4) : IDummyComponent;
record struct Component8b_215(float f0, float f4) : IDummyComponent;
record struct Component8b_216(float f0, float f4) : IDummyComponent;
record struct Component8b_217(float f0, int f4) : IDummyComponent;
record struct Component8b_218(float f0, float f4) : IDummyComponent;
record struct Component8b_219(int f0, int f4) : IDummyComponent;
record struct Component8b_220(int f0, int f4) : IDummyComponent;
record struct Component8b_221(float f0, float f4) : IDummyComponent;
record struct Component8b_222(int f0, int f4) : IDummyComponent;
record struct Component8b_223(float f0, float f4) : IDummyComponent;
record struct Component8b_224(float f0, int f4) : IDummyComponent;
record struct Component8b_225(float f0, float f4) : IDummyComponent;
record struct Component8b_226(float f0, float f4) : IDummyComponent;
record struct Component8b_227(int f0, int f4) : IDummyComponent;
record struct Component8b_228(int f0, int f4) : IDummyComponent;
record struct Component8b_229(int f0, int f4) : IDummyComponent;
record struct Component8b_230(float f0, int f4) : IDummyComponent;
record struct Component8b_231(float f0, int f4) : IDummyComponent;
record struct Component8b_232(int f0, float f4) : IDummyComponent;
record struct Component8b_233(int f0, int f4) : IDummyComponent;
record struct Component8b_234(float f0, float f4) : IDummyComponent;
record struct Component8b_235(float f0, int f4) : IDummyComponent;
record struct Component8b_236(float f0, float f4) : IDummyComponent;
record struct Component8b_237(float f0, int f4) : IDummyComponent;
record struct Component8b_238(int f0, int f4) : IDummyComponent;
record struct Component8b_239(float f0, float f4) : IDummyComponent;
record struct Component8b_240(float f0, int f4) : IDummyComponent;
record struct Component8b_241(float f0, float f4) : IDummyComponent;
record struct Component8b_242(int f0, int f4) : IDummyComponent;
record struct Component8b_243(int f0, float f4) : IDummyComponent;
record struct Component8b_244(float f0, int f4) : IDummyComponent;
record struct Component8b_245(int f0, int f4) : IDummyComponent;
record struct Component8b_246(int f0, float f4) : IDummyComponent;
record struct Component8b_247(float f0, float f4) : IDummyComponent;
record struct Component8b_248(float f0, float f4) : IDummyComponent;
record struct Component8b_249(float f0, float f4) : IDummyComponent;
record struct Component8b_250(float f0, float f4) : IDummyComponent;
record struct Component8b_251(float f0, int f4) : IDummyComponent;
record struct Component12b_0(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_1(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_2(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_3(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_4(float f0, int f4, float f8) : IDummyComponent;
record struct Component12b_5(float f0, int f4, float f8) : IDummyComponent;
record struct Component12b_6(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_7(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_8(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_9(float f0, float f4, float f8) : IDummyComponent;
record struct Component12b_10(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_11(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_12(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_13(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_14(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_15(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_16(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_17(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_18(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_19(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_20(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_21(float f0, int f4, float f8) : IDummyComponent;
record struct Component12b_22(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_23(float f0, int f4, float f8) : IDummyComponent;
record struct Component12b_24(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_25(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_26(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_27(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_28(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_29(float f0, int f4, float f8) : IDummyComponent;
record struct Component12b_30(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_31(float f0, float f4, float f8) : IDummyComponent;
record struct Component12b_32(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_33(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_34(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_35(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_36(float f0, float f4, float f8) : IDummyComponent;
record struct Component12b_37(float f0, int f4, float f8) : IDummyComponent;
record struct Component12b_38(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_39(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_40(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_41(float f0, int f4, float f8) : IDummyComponent;
record struct Component12b_42(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_43(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_44(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_45(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_46(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_47(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_48(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_49(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_50(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_51(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_52(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_53(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_54(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_55(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_56(float f0, int f4, float f8) : IDummyComponent;
record struct Component12b_57(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_58(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_59(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_60(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_61(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_62(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_63(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_64(float f0, int f4, float f8) : IDummyComponent;
record struct Component12b_65(float f0, int f4, float f8) : IDummyComponent;
record struct Component12b_66(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_67(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_68(float f0, float f4, float f8) : IDummyComponent;
record struct Component12b_69(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_70(float f0, float f4, float f8) : IDummyComponent;
record struct Component12b_71(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_72(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_73(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_74(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_75(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_76(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_77(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_78(float f0, int f4, float f8) : IDummyComponent;
record struct Component12b_79(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_80(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_81(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_82(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_83(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_84(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_85(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_86(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_87(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_88(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_89(float f0, float f4, float f8) : IDummyComponent;
record struct Component12b_90(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_91(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_92(float f0, float f4, float f8) : IDummyComponent;
record struct Component12b_93(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_94(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_95(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_96(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_97(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_98(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_99(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_100(float f0, float f4, float f8) : IDummyComponent;
record struct Component12b_101(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_102(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_103(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_104(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_105(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_106(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_107(float f0, float f4, float f8) : IDummyComponent;
record struct Component12b_108(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_109(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_110(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_111(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_112(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_113(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_114(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_115(float f0, float f4, int f8) : IDummyComponent;
record struct Component12b_116(int f0, float f4, float f8) : IDummyComponent;
record struct Component12b_117(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_118(int f0, float f4, int f8) : IDummyComponent;
record struct Component12b_119(int f0, int f4, float f8) : IDummyComponent;
record struct Component12b_120(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_121(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_122(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_123(int f0, int f4, int f8) : IDummyComponent;
record struct Component12b_124(float f0, int f4, int f8) : IDummyComponent;
record struct Component12b_125(float f0, int f4, int f8) : IDummyComponent;
record struct Component16b_0(float f0, int f4, float f8, int f12) : IDummyComponent;
record struct Component16b_1(float f0, float f4, float f8, int f12) : IDummyComponent;
record struct Component16b_2(int f0, int f4, int f8, int f12) : IDummyComponent;
record struct Component16b_3(int f0, int f4, float f8, float f12) : IDummyComponent;
record struct Component16b_4(int f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_5(float f0, float f4, int f8, float f12) : IDummyComponent;
record struct Component16b_6(int f0, int f4, int f8, int f12) : IDummyComponent;
record struct Component16b_7(float f0, float f4, int f8, float f12) : IDummyComponent;
record struct Component16b_8(int f0, int f4, int f8, float f12) : IDummyComponent;
record struct Component16b_9(int f0, int f4, float f8, float f12) : IDummyComponent;
record struct Component16b_10(float f0, int f4, int f8, int f12) : IDummyComponent;
record struct Component16b_11(int f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_12(int f0, int f4, int f8, float f12) : IDummyComponent;
record struct Component16b_13(int f0, float f4, int f8, float f12) : IDummyComponent;
record struct Component16b_14(float f0, int f4, int f8, float f12) : IDummyComponent;
record struct Component16b_15(float f0, float f4, int f8, int f12) : IDummyComponent;
record struct Component16b_16(int f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_17(int f0, float f4, int f8, float f12) : IDummyComponent;
record struct Component16b_18(int f0, int f4, float f8, float f12) : IDummyComponent;
record struct Component16b_19(float f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_20(int f0, float f4, int f8, float f12) : IDummyComponent;
record struct Component16b_21(int f0, int f4, float f8, float f12) : IDummyComponent;
record struct Component16b_22(int f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_23(float f0, float f4, int f8, float f12) : IDummyComponent;
record struct Component16b_24(int f0, int f4, int f8, float f12) : IDummyComponent;
record struct Component16b_25(float f0, float f4, int f8, int f12) : IDummyComponent;
record struct Component16b_26(int f0, int f4, int f8, int f12) : IDummyComponent;
record struct Component16b_27(float f0, int f4, float f8, int f12) : IDummyComponent;
record struct Component16b_28(int f0, int f4, int f8, float f12) : IDummyComponent;
record struct Component16b_29(float f0, float f4, int f8, int f12) : IDummyComponent;
record struct Component16b_30(int f0, int f4, float f8, float f12) : IDummyComponent;
record struct Component16b_31(float f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_32(float f0, int f4, int f8, float f12) : IDummyComponent;
record struct Component16b_33(float f0, int f4, float f8, float f12) : IDummyComponent;
record struct Component16b_34(int f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_35(int f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_36(float f0, int f4, int f8, int f12) : IDummyComponent;
record struct Component16b_37(int f0, int f4, int f8, int f12) : IDummyComponent;
record struct Component16b_38(float f0, int f4, int f8, int f12) : IDummyComponent;
record struct Component16b_39(float f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_40(float f0, int f4, int f8, float f12) : IDummyComponent;
record struct Component16b_41(float f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_42(int f0, float f4, int f8, int f12) : IDummyComponent;
record struct Component16b_43(float f0, float f4, int f8, float f12) : IDummyComponent;
record struct Component16b_44(int f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_45(int f0, int f4, float f8, float f12) : IDummyComponent;
record struct Component16b_46(float f0, float f4, int f8, int f12) : IDummyComponent;
record struct Component16b_47(float f0, float f4, int f8, float f12) : IDummyComponent;
record struct Component16b_48(int f0, int f4, float f8, float f12) : IDummyComponent;
record struct Component16b_49(int f0, float f4, int f8, float f12) : IDummyComponent;
record struct Component16b_50(float f0, float f4, int f8, float f12) : IDummyComponent;
record struct Component16b_51(int f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_52(int f0, float f4, int f8, int f12) : IDummyComponent;
record struct Component16b_53(float f0, float f4, int f8, float f12) : IDummyComponent;
record struct Component16b_54(float f0, int f4, float f8, float f12) : IDummyComponent;
record struct Component16b_55(int f0, int f4, int f8, float f12) : IDummyComponent;
record struct Component16b_56(float f0, float f4, int f8, float f12) : IDummyComponent;
record struct Component16b_57(float f0, float f4, int f8, int f12) : IDummyComponent;
record struct Component16b_58(float f0, int f4, float f8, int f12) : IDummyComponent;
record struct Component16b_59(float f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_60(float f0, int f4, int f8, float f12) : IDummyComponent;
record struct Component16b_61(int f0, int f4, float f8, int f12) : IDummyComponent;
record struct Component16b_62(int f0, int f4, float f8, int f12) : IDummyComponent;
record struct Component16b_63(int f0, float f4, int f8, float f12) : IDummyComponent;
record struct Component16b_64(int f0, int f4, int f8, float f12) : IDummyComponent;
record struct Component16b_65(float f0, int f4, float f8, int f12) : IDummyComponent;
record struct Component16b_66(int f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_67(int f0, int f4, int f8, int f12) : IDummyComponent;
record struct Component16b_68(float f0, int f4, int f8, float f12) : IDummyComponent;
record struct Component16b_69(float f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_70(int f0, int f4, int f8, float f12) : IDummyComponent;
record struct Component16b_71(float f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_72(int f0, float f4, int f8, float f12) : IDummyComponent;
record struct Component16b_73(float f0, float f4, int f8, int f12) : IDummyComponent;
record struct Component16b_74(float f0, float f4, int f8, int f12) : IDummyComponent;
record struct Component16b_75(float f0, int f4, float f8, float f12) : IDummyComponent;
record struct Component16b_76(float f0, int f4, float f8, int f12) : IDummyComponent;
record struct Component16b_77(float f0, float f4, int f8, int f12) : IDummyComponent;
record struct Component16b_78(float f0, float f4, int f8, int f12) : IDummyComponent;
record struct Component16b_79(int f0, float f4, int f8, int f12) : IDummyComponent;
record struct Component16b_80(float f0, int f4, int f8, int f12) : IDummyComponent;
record struct Component16b_81(int f0, float f4, float f8, int f12) : IDummyComponent;
record struct Component16b_82(float f0, float f4, float f8, float f12) : IDummyComponent;
record struct Component16b_83(float f0, int f4, int f8, int f12) : IDummyComponent;
record struct Component20b_0(float f0, int f4, int f8, int f12, float f16) : IDummyComponent;
record struct Component20b_1(int f0, int f4, float f8, float f12, float f16) : IDummyComponent;
record struct Component20b_2(float f0, float f4, float f8, float f12, int f16) : IDummyComponent;
record struct Component20b_3(int f0, float f4, float f8, int f12, int f16) : IDummyComponent;
record struct Component20b_4(float f0, int f4, float f8, float f12, int f16) : IDummyComponent;
record struct Component20b_5(float f0, int f4, int f8, int f12, float f16) : IDummyComponent;
record struct Component20b_6(float f0, int f4, float f8, float f12, float f16) : IDummyComponent;
record struct Component20b_7(int f0, float f4, float f8, int f12, float f16) : IDummyComponent;
record struct Component20b_8(float f0, float f4, int f8, int f12, float f16) : IDummyComponent;
record struct Component20b_9(float f0, int f4, float f8, int f12, int f16) : IDummyComponent;
record struct Component20b_10(int f0, float f4, int f8, float f12, int f16) : IDummyComponent;
record struct Component20b_11(float f0, int f4, int f8, int f12, float f16) : IDummyComponent;
record struct Component20b_12(int f0, float f4, float f8, float f12, float f16) : IDummyComponent;
record struct Component20b_13(int f0, float f4, float f8, float f12, float f16) : IDummyComponent;
record struct Component20b_14(float f0, int f4, float f8, float f12, int f16) : IDummyComponent;
record struct Component20b_15(float f0, float f4, float f8, int f12, int f16) : IDummyComponent;
record struct Component20b_16(float f0, float f4, int f8, float f12, float f16) : IDummyComponent;
record struct Component20b_17(int f0, int f4, int f8, float f12, float f16) : IDummyComponent;
record struct Component20b_18(float f0, int f4, float f8, int f12, float f16) : IDummyComponent;
record struct Component20b_19(float f0, int f4, float f8, float f12, float f16) : IDummyComponent;
record struct Component20b_20(float f0, float f4, int f8, int f12, float f16) : IDummyComponent;
record struct Component20b_21(int f0, float f4, float f8, float f12, int f16) : IDummyComponent;
record struct Component20b_22(int f0, float f4, float f8, int f12, float f16) : IDummyComponent;
record struct Component20b_23(float f0, int f4, int f8, float f12, float f16) : IDummyComponent;
record struct Component20b_24(int f0, float f4, float f8, int f12, int f16) : IDummyComponent;
record struct Component20b_25(int f0, float f4, float f8, float f12, float f16) : IDummyComponent;
record struct Component20b_26(int f0, int f4, int f8, float f12, float f16) : IDummyComponent;
record struct Component20b_27(int f0, int f4, float f8, float f12, int f16) : IDummyComponent;
record struct Component20b_28(int f0, float f4, int f8, int f12, int f16) : IDummyComponent;
record struct Component20b_29(float f0, float f4, float f8, int f12, float f16) : IDummyComponent;
record struct Component20b_30(int f0, int f4, int f8, int f12, float f16) : IDummyComponent;
record struct Component20b_31(float f0, float f4, int f8, float f12, float f16) : IDummyComponent;
record struct Component20b_32(float f0, float f4, float f8, int f12, int f16) : IDummyComponent;
record struct Component20b_33(int f0, float f4, float f8, int f12, int f16) : IDummyComponent;
record struct Component20b_34(int f0, int f4, int f8, int f12, float f16) : IDummyComponent;
record struct Component20b_35(int f0, int f4, float f8, float f12, float f16) : IDummyComponent;
record struct Component20b_36(float f0, int f4, int f8, float f12, int f16) : IDummyComponent;
record struct Component20b_37(float f0, float f4, float f8, float f12, float f16) : IDummyComponent;
record struct Component20b_38(int f0, int f4, float f8, float f12, int f16) : IDummyComponent;
record struct Component20b_39(float f0, int f4, float f8, int f12, float f16) : IDummyComponent;
record struct Component20b_40(float f0, int f4, float f8, float f12, float f16) : IDummyComponent;
record struct Component20b_41(float f0, int f4, int f8, float f12, float f16) : IDummyComponent;
record struct Component20b_42(float f0, int f4, float f8, int f12, float f16) : IDummyComponent;
record struct Component20b_43(float f0, int f4, float f8, float f12, int f16) : IDummyComponent;
record struct Component20b_44(float f0, float f4, float f8, float f12, int f16) : IDummyComponent;
record struct Component20b_45(int f0, int f4, int f8, float f12, float f16) : IDummyComponent;
record struct Component20b_46(int f0, int f4, int f8, int f12, float f16) : IDummyComponent;
record struct Component20b_47(int f0, int f4, int f8, float f12, float f16) : IDummyComponent;
record struct Component20b_48(float f0, float f4, float f8, float f12, int f16) : IDummyComponent;
record struct Component20b_49(float f0, int f4, int f8, float f12, int f16) : IDummyComponent;
record struct Component20b_50(int f0, float f4, float f8, float f12, float f16) : IDummyComponent;
record struct Component20b_51(int f0, float f4, int f8, float f12, int f16) : IDummyComponent;
record struct Component20b_52(int f0, int f4, int f8, int f12, int f16) : IDummyComponent;
record struct Component20b_53(float f0, int f4, float f8, int f12, float f16) : IDummyComponent;
record struct Component20b_54(int f0, int f4, float f8, float f12, int f16) : IDummyComponent;
record struct Component20b_55(int f0, float f4, int f8, int f12, int f16) : IDummyComponent;
record struct Component20b_56(float f0, int f4, int f8, int f12, int f16) : IDummyComponent;
record struct Component20b_57(float f0, int f4, float f8, int f12, float f16) : IDummyComponent;
record struct Component20b_58(int f0, float f4, float f8, int f12, int f16) : IDummyComponent;
record struct Component20b_59(float f0, int f4, int f8, int f12, int f16) : IDummyComponent;
record struct Component20b_60(float f0, int f4, float f8, int f12, float f16) : IDummyComponent;
record struct Component20b_61(int f0, float f4, int f8, int f12, float f16) : IDummyComponent;
record struct Component20b_62(int f0, int f4, int f8, float f12, float f16) : IDummyComponent;
record struct Component24b_0(float f0, int f4, int f8, float f12, float f16, float f20) : IDummyComponent;
record struct Component24b_1(int f0, int f4, int f8, float f12, float f16, float f20) : IDummyComponent;
record struct Component24b_2(int f0, float f4, float f8, float f12, float f16, int f20) : IDummyComponent;
record struct Component24b_3(int f0, int f4, int f8, int f12, float f16, int f20) : IDummyComponent;
record struct Component24b_4(int f0, int f4, int f8, int f12, float f16, float f20) : IDummyComponent;
record struct Component24b_5(int f0, float f4, float f8, float f12, float f16, float f20) : IDummyComponent;
record struct Component24b_6(float f0, float f4, float f8, float f12, int f16, int f20) : IDummyComponent;
record struct Component24b_7(float f0, float f4, int f8, float f12, float f16, int f20) : IDummyComponent;
record struct Component24b_8(int f0, int f4, int f8, int f12, int f16, int f20) : IDummyComponent;
record struct Component24b_9(int f0, int f4, int f8, int f12, int f16, float f20) : IDummyComponent;
record struct Component24b_10(int f0, float f4, float f8, int f12, int f16, float f20) : IDummyComponent;
record struct Component24b_11(float f0, float f4, float f8, int f12, float f16, int f20) : IDummyComponent;
record struct Component24b_12(float f0, float f4, float f8, int f12, float f16, float f20) : IDummyComponent;
record struct Component24b_13(float f0, float f4, float f8, float f12, float f16, int f20) : IDummyComponent;
record struct Component24b_14(int f0, float f4, float f8, int f12, int f16, int f20) : IDummyComponent;
record struct Component24b_15(float f0, float f4, int f8, float f12, int f16, int f20) : IDummyComponent;
record struct Component24b_16(int f0, float f4, int f8, int f12, int f16, int f20) : IDummyComponent;
record struct Component24b_17(int f0, float f4, float f8, float f12, float f16, int f20) : IDummyComponent;
record struct Component24b_18(float f0, int f4, float f8, int f12, float f16, int f20) : IDummyComponent;
record struct Component24b_19(int f0, float f4, float f8, int f12, float f16, float f20) : IDummyComponent;
record struct Component24b_20(int f0, float f4, float f8, float f12, float f16, float f20) : IDummyComponent;
record struct Component24b_21(int f0, float f4, float f8, float f12, float f16, float f20) : IDummyComponent;
record struct Component24b_22(int f0, float f4, float f8, float f12, int f16, int f20) : IDummyComponent;
record struct Component24b_23(float f0, float f4, int f8, int f12, float f16, int f20) : IDummyComponent;
record struct Component24b_24(float f0, int f4, int f8, int f12, float f16, float f20) : IDummyComponent;
record struct Component24b_25(float f0, int f4, float f8, int f12, float f16, float f20) : IDummyComponent;
record struct Component24b_26(int f0, int f4, int f8, int f12, int f16, int f20) : IDummyComponent;
record struct Component24b_27(float f0, int f4, int f8, float f12, int f16, int f20) : IDummyComponent;
record struct Component24b_28(int f0, int f4, float f8, int f12, int f16, float f20) : IDummyComponent;
record struct Component24b_29(float f0, float f4, float f8, int f12, int f16, int f20) : IDummyComponent;
record struct Component24b_30(int f0, float f4, float f8, int f12, float f16, int f20) : IDummyComponent;
record struct Component24b_31(int f0, int f4, int f8, float f12, float f16, float f20) : IDummyComponent;
record struct Component24b_32(float f0, float f4, int f8, float f12, int f16, int f20) : IDummyComponent;
record struct Component24b_33(int f0, float f4, float f8, int f12, float f16, float f20) : IDummyComponent;
record struct Component24b_34(int f0, int f4, int f8, int f12, int f16, int f20) : IDummyComponent;
record struct Component24b_35(float f0, float f4, int f8, float f12, int f16, float f20) : IDummyComponent;
record struct Component24b_36(float f0, float f4, int f8, float f12, int f16, int f20) : IDummyComponent;
record struct Component24b_37(float f0, float f4, float f8, float f12, int f16, float f20) : IDummyComponent;
record struct Component24b_38(float f0, float f4, int f8, int f12, int f16, float f20) : IDummyComponent;
record struct Component24b_39(int f0, int f4, int f8, float f12, float f16, float f20) : IDummyComponent;
record struct Component24b_40(int f0, float f4, int f8, float f12, float f16, int f20) : IDummyComponent;
record struct Component24b_41(int f0, int f4, float f8, int f12, int f16, float f20) : IDummyComponent;
record struct Component24b_42(int f0, float f4, int f8, int f12, float f16, int f20) : IDummyComponent;
record struct Component24b_43(int f0, float f4, int f8, float f12, int f16, int f20) : IDummyComponent;
record struct Component24b_44(float f0, float f4, int f8, float f12, int f16, float f20) : IDummyComponent;
record struct Component24b_45(int f0, int f4, int f8, int f12, float f16, int f20) : IDummyComponent;
record struct Component24b_46(int f0, float f4, int f8, float f12, float f16, int f20) : IDummyComponent;
record struct Component24b_47(float f0, float f4, float f8, int f12, float f16, int f20) : IDummyComponent;
record struct Component24b_48(float f0, int f4, int f8, int f12, float f16, int f20) : IDummyComponent;
record struct Component24b_49(int f0, float f4, float f8, float f12, int f16, int f20) : IDummyComponent;
record struct Component28b_0(float f0, float f4, float f8, int f12, int f16, int f20, float f24) : IDummyComponent;
record struct Component28b_1(int f0, int f4, float f8, int f12, float f16, float f20, float f24) : IDummyComponent;
record struct Component28b_2(float f0, float f4, int f8, int f12, int f16, int f20, float f24) : IDummyComponent;
record struct Component28b_3(int f0, float f4, float f8, int f12, int f16, int f20, float f24) : IDummyComponent;
record struct Component28b_4(int f0, int f4, float f8, int f12, float f16, int f20, float f24) : IDummyComponent;
record struct Component28b_5(float f0, int f4, int f8, float f12, int f16, int f20, int f24) : IDummyComponent;
record struct Component28b_6(int f0, int f4, float f8, float f12, int f16, float f20, float f24) : IDummyComponent;
record struct Component28b_7(float f0, float f4, int f8, int f12, float f16, int f20, int f24) : IDummyComponent;
record struct Component28b_8(float f0, float f4, float f8, float f12, float f16, float f20, int f24) : IDummyComponent;
record struct Component28b_9(float f0, int f4, int f8, int f12, float f16, float f20, float f24) : IDummyComponent;
record struct Component28b_10(float f0, float f4, float f8, int f12, int f16, int f20, float f24) : IDummyComponent;
record struct Component28b_11(int f0, float f4, float f8, float f12, int f16, int f20, int f24) : IDummyComponent;
record struct Component28b_12(int f0, int f4, float f8, float f12, int f16, float f20, int f24) : IDummyComponent;
record struct Component28b_13(float f0, float f4, int f8, float f12, float f16, int f20, int f24) : IDummyComponent;
record struct Component28b_14(float f0, float f4, float f8, float f12, int f16, float f20, float f24) : IDummyComponent;
record struct Component28b_15(int f0, int f4, int f8, float f12, float f16, float f20, float f24) : IDummyComponent;
record struct Component28b_16(int f0, float f4, int f8, float f12, int f16, float f20, float f24) : IDummyComponent;
record struct Component28b_17(float f0, int f4, float f8, float f12, float f16, int f20, int f24) : IDummyComponent;
record struct Component28b_18(int f0, int f4, float f8, float f12, float f16, int f20, float f24) : IDummyComponent;
record struct Component28b_19(int f0, float f4, int f8, int f12, float f16, float f20, float f24) : IDummyComponent;
record struct Component28b_20(int f0, int f4, int f8, int f12, float f16, float f20, float f24) : IDummyComponent;
record struct Component28b_21(float f0, int f4, float f8, int f12, float f16, int f20, float f24) : IDummyComponent;
record struct Component28b_22(int f0, int f4, float f8, float f12, float f16, int f20, int f24) : IDummyComponent;
record struct Component28b_23(int f0, float f4, int f8, float f12, float f16, int f20, float f24) : IDummyComponent;
record struct Component28b_24(int f0, float f4, float f8, float f12, int f16, int f20, float f24) : IDummyComponent;
record struct Component28b_25(int f0, float f4, float f8, int f12, float f16, float f20, float f24) : IDummyComponent;
record struct Component28b_26(float f0, int f4, float f8, int f12, int f16, float f20, float f24) : IDummyComponent;
record struct Component28b_27(int f0, int f4, float f8, int f12, int f16, int f20, float f24) : IDummyComponent;
record struct Component28b_28(int f0, float f4, int f8, int f12, int f16, int f20, int f24) : IDummyComponent;
record struct Component28b_29(float f0, float f4, float f8, float f12, int f16, int f20, float f24) : IDummyComponent;
record struct Component28b_30(int f0, float f4, float f8, int f12, int f16, int f20, int f24) : IDummyComponent;
record struct Component28b_31(int f0, int f4, float f8, float f12, int f16, int f20, float f24) : IDummyComponent;
record struct Component28b_32(int f0, float f4, float f8, float f12, float f16, float f20, float f24) : IDummyComponent;
record struct Component28b_33(float f0, float f4, float f8, float f12, int f16, float f20, int f24) : IDummyComponent;
record struct Component28b_34(int f0, float f4, int f8, float f12, int f16, float f20, float f24) : IDummyComponent;
record struct Component28b_35(float f0, int f4, int f8, float f12, int f16, float f20, int f24) : IDummyComponent;
record struct Component28b_36(int f0, float f4, int f8, int f12, float f16, int f20, float f24) : IDummyComponent;
record struct Component28b_37(float f0, float f4, float f8, int f12, float f16, int f20, int f24) : IDummyComponent;
record struct Component28b_38(int f0, float f4, int f8, int f12, float f16, float f20, float f24) : IDummyComponent;
record struct Component28b_39(int f0, float f4, float f8, float f12, float f16, int f20, float f24) : IDummyComponent;
record struct Component28b_40(float f0, int f4, int f8, float f12, int f16, int f20, int f24) : IDummyComponent;
record struct Component28b_41(float f0, float f4, int f8, float f12, float f16, int f20, int f24) : IDummyComponent;
record struct Component32b_0(int f0, float f4, int f8, float f12, int f16, float f20, int f24, int f28) : IDummyComponent;
record struct Component32b_1(float f0, float f4, int f8, int f12, float f16, float f20, float f24, float f28) : IDummyComponent;
record struct Component32b_2(int f0, float f4, float f8, float f12, float f16, int f20, int f24, float f28) : IDummyComponent;
record struct Component32b_3(float f0, float f4, int f8, int f12, int f16, int f20, int f24, float f28) : IDummyComponent;
record struct Component32b_4(float f0, float f4, int f8, int f12, int f16, float f20, float f24, float f28) : IDummyComponent;
record struct Component32b_5(int f0, float f4, int f8, float f12, int f16, float f20, int f24, int f28) : IDummyComponent;
record struct Component32b_6(float f0, int f4, int f8, int f12, float f16, float f20, int f24, int f28) : IDummyComponent;
record struct Component32b_7(int f0, float f4, float f8, float f12, float f16, float f20, int f24, float f28) : IDummyComponent;
record struct Component32b_8(int f0, float f4, int f8, int f12, float f16, float f20, int f24, int f28) : IDummyComponent;
record struct Component32b_9(float f0, float f4, float f8, int f12, int f16, int f20, int f24, float f28) : IDummyComponent;
record struct Component32b_10(float f0, int f4, int f8, int f12, int f16, int f20, int f24, float f28) : IDummyComponent;
record struct Component32b_11(float f0, int f4, int f8, int f12, int f16, int f20, int f24, float f28) : IDummyComponent;
record struct Component32b_12(float f0, float f4, float f8, int f12, int f16, float f20, int f24, int f28) : IDummyComponent;
record struct Component32b_13(float f0, int f4, float f8, float f12, float f16, float f20, int f24, float f28) : IDummyComponent;
record struct Component32b_14(float f0, int f4, int f8, float f12, float f16, float f20, float f24, int f28) : IDummyComponent;
record struct Component32b_15(int f0, float f4, float f8, float f12, int f16, int f20, float f24, float f28) : IDummyComponent;
record struct Component32b_16(float f0, float f4, int f8, int f12, int f16, float f20, float f24, int f28) : IDummyComponent;
record struct Component32b_17(float f0, float f4, float f8, float f12, int f16, float f20, int f24, float f28) : IDummyComponent;
record struct Component32b_18(float f0, int f4, float f8, float f12, float f16, float f20, float f24, float f28) : IDummyComponent;
record struct Component32b_19(int f0, int f4, int f8, int f12, int f16, int f20, int f24, int f28) : IDummyComponent;
record struct Component32b_20(float f0, float f4, float f8, int f12, float f16, float f20, int f24, int f28) : IDummyComponent;
record struct Component32b_21(float f0, int f4, int f8, float f12, float f16, int f20, float f24, float f28) : IDummyComponent;
record struct Component32b_22(float f0, float f4, int f8, float f12, float f16, float f20, float f24, float f28) : IDummyComponent;
record struct Component32b_23(float f0, float f4, int f8, int f12, int f16, int f20, int f24, float f28) : IDummyComponent;
record struct Component32b_24(int f0, int f4, int f8, float f12, float f16, int f20, int f24, float f28) : IDummyComponent;
record struct Component32b_25(float f0, float f4, int f8, int f12, float f16, float f20, float f24, float f28) : IDummyComponent;
record struct Component32b_26(int f0, int f4, float f8, float f12, float f16, float f20, float f24, float f28) : IDummyComponent;
record struct Component32b_27(int f0, float f4, float f8, int f12, int f16, float f20, float f24, float f28) : IDummyComponent;
record struct Component32b_28(int f0, float f4, float f8, int f12, float f16, float f20, int f24, int f28) : IDummyComponent;
record struct Component32b_29(float f0, float f4, int f8, float f12, int f16, int f20, float f24, float f28) : IDummyComponent;
record struct Component32b_30(int f0, float f4, int f8, int f12, int f16, int f20, int f24, int f28) : IDummyComponent;
record struct Component32b_31(int f0, float f4, int f8, float f12, float f16, float f20, float f24, float f28) : IDummyComponent;
record struct Component32b_32(float f0, float f4, int f8, float f12, float f16, int f20, float f24, int f28) : IDummyComponent;
record struct Component32b_33(int f0, int f4, int f8, int f12, float f16, int f20, float f24, float f28) : IDummyComponent;
record struct Component32b_34(int f0, float f4, float f8, float f12, float f16, float f20, int f24, float f28) : IDummyComponent;
record struct Component32b_35(int f0, int f4, int f8, int f12, int f16, float f20, int f24, float f28) : IDummyComponent;
record struct Component36b_0(float f0, int f4, float f8, float f12, int f16, float f20, float f24, int f28, float f32) : IDummyComponent;
record struct Component36b_1(float f0, int f4, float f8, int f12, float f16, int f20, int f24, int f28, int f32) : IDummyComponent;
record struct Component36b_2(float f0, int f4, float f8, float f12, int f16, float f20, float f24, int f28, float f32) : IDummyComponent;
record struct Component36b_3(float f0, int f4, int f8, float f12, int f16, float f20, float f24, float f28, float f32) : IDummyComponent;
record struct Component36b_4(float f0, float f4, float f8, float f12, int f16, int f20, int f24, float f28, int f32) : IDummyComponent;
record struct Component36b_5(float f0, int f4, int f8, int f12, int f16, float f20, int f24, int f28, int f32) : IDummyComponent;
record struct Component36b_6(int f0, int f4, int f8, float f12, int f16, int f20, int f24, float f28, float f32) : IDummyComponent;
record struct Component36b_7(int f0, int f4, float f8, int f12, int f16, float f20, int f24, int f28, float f32) : IDummyComponent;
record struct Component36b_8(float f0, float f4, float f8, int f12, int f16, float f20, float f24, int f28, float f32) : IDummyComponent;
record struct Component36b_9(int f0, float f4, int f8, float f12, int f16, int f20, float f24, float f28, int f32) : IDummyComponent;
record struct Component36b_10(int f0, int f4, int f8, int f12, float f16, int f20, float f24, int f28, float f32) : IDummyComponent;
record struct Component36b_11(float f0, int f4, float f8, float f12, float f16, int f20, float f24, int f28, float f32) : IDummyComponent;
record struct Component36b_12(float f0, float f4, float f8, float f12, int f16, int f20, float f24, int f28, int f32) : IDummyComponent;
record struct Component36b_13(int f0, int f4, int f8, float f12, int f16, float f20, float f24, int f28, float f32) : IDummyComponent;
record struct Component36b_14(int f0, int f4, int f8, float f12, int f16, float f20, int f24, int f28, int f32) : IDummyComponent;
record struct Component36b_15(int f0, float f4, int f8, float f12, float f16, float f20, float f24, int f28, float f32) : IDummyComponent;
record struct Component36b_16(int f0, float f4, float f8, float f12, int f16, int f20, float f24, float f28, float f32) : IDummyComponent;
record struct Component36b_17(int f0, float f4, float f8, float f12, float f16, float f20, float f24, float f28, int f32) : IDummyComponent;
record struct Component36b_18(float f0, float f4, float f8, float f12, float f16, float f20, float f24, float f28, float f32) : IDummyComponent;
record struct Component36b_19(int f0, float f4, int f8, float f12, int f16, float f20, int f24, float f28, int f32) : IDummyComponent;
record struct Component36b_20(int f0, float f4, float f8, int f12, float f16, float f20, int f24, int f28, float f32) : IDummyComponent;
record struct Component36b_21(int f0, int f4, int f8, float f12, int f16, float f20, float f24, int f28, float f32) : IDummyComponent;
record struct Component36b_22(int f0, int f4, float f8, int f12, int f16, float f20, float f24, float f28, float f32) : IDummyComponent;
record struct Component36b_23(float f0, float f4, float f8, int f12, int f16, float f20, float f24, int f28, float f32) : IDummyComponent;
record struct Component36b_24(int f0, int f4, int f8, float f12, int f16, int f20, float f24, int f28, float f32) : IDummyComponent;
record struct Component36b_25(int f0, float f4, int f8, int f12, float f16, float f20, float f24, float f28, float f32) : IDummyComponent;
record struct Component36b_26(float f0, int f4, float f8, int f12, float f16, float f20, int f24, float f28, float f32) : IDummyComponent;
record struct Component36b_27(int f0, float f4, int f8, int f12, int f16, float f20, int f24, int f28, float f32) : IDummyComponent;
record struct Component36b_28(float f0, int f4, float f8, float f12, float f16, int f20, float f24, float f28, int f32) : IDummyComponent;
record struct Component36b_29(float f0, int f4, float f8, int f12, int f16, int f20, float f24, float f28, int f32) : IDummyComponent;
record struct Component36b_30(int f0, int f4, int f8, int f12, int f16, int f20, int f24, float f28, int f32) : IDummyComponent;
record struct Component40b_0(int f0, float f4, float f8, float f12, int f16, int f20, int f24, int f28, int f32, int f36) : IDummyComponent;
record struct Component40b_1(int f0, int f4, float f8, float f12, int f16, float f20, float f24, float f28, int f32, float f36) : IDummyComponent;
record struct Component40b_2(float f0, float f4, float f8, int f12, float f16, int f20, float f24, float f28, int f32, float f36) : IDummyComponent;
record struct Component40b_3(int f0, int f4, float f8, int f12, int f16, float f20, float f24, float f28, float f32, float f36) : IDummyComponent;
record struct Component40b_4(int f0, int f4, int f8, float f12, int f16, float f20, float f24, int f28, float f32, int f36) : IDummyComponent;
record struct Component40b_5(float f0, int f4, float f8, int f12, float f16, int f20, float f24, float f28, float f32, int f36) : IDummyComponent;
record struct Component40b_6(float f0, float f4, float f8, int f12, float f16, int f20, float f24, int f28, float f32, int f36) : IDummyComponent;
record struct Component40b_7(int f0, int f4, int f8, float f12, float f16, int f20, float f24, float f28, float f32, int f36) : IDummyComponent;
record struct Component40b_8(int f0, int f4, float f8, float f12, float f16, float f20, float f24, int f28, int f32, int f36) : IDummyComponent;
record struct Component40b_9(float f0, int f4, float f8, float f12, float f16, int f20, int f24, float f28, float f32, float f36) : IDummyComponent;
record struct Component40b_10(int f0, float f4, int f8, int f12, float f16, float f20, int f24, float f28, float f32, int f36) : IDummyComponent;
record struct Component40b_11(int f0, int f4, float f8, float f12, float f16, float f20, int f24, int f28, int f32, float f36) : IDummyComponent;
record struct Component40b_12(int f0, int f4, int f8, float f12, int f16, int f20, int f24, float f28, float f32, float f36) : IDummyComponent;
record struct Component40b_13(float f0, int f4, int f8, float f12, float f16, int f20, float f24, int f28, float f32, int f36) : IDummyComponent;
record struct Component40b_14(float f0, float f4, int f8, int f12, float f16, float f20, int f24, int f28, int f32, int f36) : IDummyComponent;
record struct Component40b_15(float f0, float f4, int f8, int f12, float f16, float f20, int f24, int f28, int f32, float f36) : IDummyComponent;
record struct Component40b_16(float f0, float f4, float f8, int f12, float f16, int f20, int f24, float f28, int f32, int f36) : IDummyComponent;
record struct Component40b_17(float f0, int f4, int f8, int f12, int f16, float f20, float f24, int f28, int f32, float f36) : IDummyComponent;
record struct Component40b_18(int f0, int f4, float f8, int f12, float f16, int f20, int f24, int f28, int f32, int f36) : IDummyComponent;
record struct Component40b_19(int f0, float f4, float f8, float f12, int f16, float f20, float f24, int f28, int f32, float f36) : IDummyComponent;
record struct Component40b_20(int f0, int f4, int f8, int f12, int f16, int f20, float f24, int f28, float f32, int f36) : IDummyComponent;
record struct Component40b_21(int f0, int f4, float f8, float f12, int f16, float f20, float f24, float f28, float f32, int f36) : IDummyComponent;
record struct Component40b_22(float f0, float f4, int f8, int f12, float f16, int f20, int f24, float f28, int f32, float f36) : IDummyComponent;
record struct Component40b_23(float f0, float f4, float f8, int f12, int f16, float f20, float f24, float f28, int f32, int f36) : IDummyComponent;
record struct Component40b_24(float f0, int f4, int f8, float f12, int f16, int f20, float f24, float f28, float f32, int f36) : IDummyComponent;
record struct Component40b_25(int f0, int f4, float f8, int f12, float f16, float f20, float f24, int f28, int f32, int f36) : IDummyComponent;
record struct Component40b_26(int f0, float f4, float f8, float f12, int f16, int f20, int f24, int f28, float f32, float f36) : IDummyComponent;
record struct Component40b_27(float f0, int f4, int f8, float f12, int f16, float f20, int f24, float f28, int f32, float f36) : IDummyComponent;
record struct Component44b_0(float f0, float f4, int f8, float f12, float f16, int f20, int f24, int f28, int f32, float f36, int f40) : IDummyComponent;
record struct Component44b_1(int f0, float f4, float f8, int f12, float f16, int f20, float f24, float f28, int f32, float f36, int f40) : IDummyComponent;
record struct Component44b_2(float f0, float f4, int f8, int f12, int f16, float f20, int f24, int f28, int f32, float f36, float f40) : IDummyComponent;
record struct Component44b_3(int f0, float f4, float f8, int f12, int f16, float f20, float f24, int f28, int f32, int f36, int f40) : IDummyComponent;
record struct Component44b_4(int f0, float f4, int f8, int f12, float f16, float f20, int f24, float f28, float f32, int f36, int f40) : IDummyComponent;
record struct Component44b_5(float f0, int f4, int f8, float f12, int f16, int f20, int f24, float f28, float f32, int f36, int f40) : IDummyComponent;
record struct Component44b_6(float f0, float f4, int f8, float f12, int f16, float f20, float f24, int f28, int f32, float f36, float f40) : IDummyComponent;
record struct Component44b_7(int f0, float f4, int f8, int f12, float f16, float f20, int f24, float f28, float f32, float f36, float f40) : IDummyComponent;
record struct Component44b_8(float f0, float f4, int f8, float f12, int f16, int f20, float f24, float f28, int f32, int f36, float f40) : IDummyComponent;
record struct Component44b_9(float f0, int f4, int f8, int f12, int f16, float f20, int f24, int f28, int f32, int f36, int f40) : IDummyComponent;
record struct Component44b_10(float f0, float f4, int f8, int f12, int f16, int f20, int f24, int f28, float f32, float f36, int f40) : IDummyComponent;
record struct Component44b_11(int f0, float f4, float f8, float f12, int f16, int f20, float f24, float f28, int f32, float f36, float f40) : IDummyComponent;
record struct Component44b_12(float f0, int f4, int f8, int f12, float f16, int f20, int f24, float f28, float f32, float f36, float f40) : IDummyComponent;
record struct Component44b_13(float f0, float f4, float f8, int f12, int f16, int f20, float f24, int f28, float f32, int f36, float f40) : IDummyComponent;
record struct Component44b_14(int f0, int f4, float f8, float f12, float f16, float f20, float f24, int f28, float f32, float f36, float f40) : IDummyComponent;
record struct Component44b_15(float f0, int f4, float f8, float f12, float f16, float f20, float f24, int f28, int f32, int f36, float f40) : IDummyComponent;
record struct Component44b_16(int f0, float f4, int f8, int f12, float f16, int f20, float f24, int f28, float f32, float f36, float f40) : IDummyComponent;
record struct Component44b_17(float f0, float f4, int f8, int f12, int f16, float f20, int f24, int f28, float f32, float f36, int f40) : IDummyComponent;
record struct Component44b_18(int f0, float f4, float f8, int f12, float f16, int f20, int f24, float f28, int f32, int f36, float f40) : IDummyComponent;
record struct Component44b_19(float f0, int f4, float f8, int f12, int f16, float f20, int f24, int f28, int f32, int f36, int f40) : IDummyComponent;
record struct Component44b_20(int f0, float f4, float f8, int f12, int f16, int f20, float f24, int f28, int f32, float f36, float f40) : IDummyComponent;
record struct Component44b_21(float f0, float f4, float f8, float f12, int f16, float f20, int f24, int f28, float f32, int f36, int f40) : IDummyComponent;
record struct Component44b_22(int f0, int f4, int f8, int f12, float f16, int f20, int f24, float f28, float f32, int f36, int f40) : IDummyComponent;
record struct Component44b_23(float f0, float f4, int f8, float f12, float f16, float f20, int f24, float f28, int f32, float f36, int f40) : IDummyComponent;
record struct Component44b_24(float f0, float f4, float f8, int f12, int f16, int f20, float f24, float f28, int f32, int f36, float f40) : IDummyComponent;
record struct Component48b_0(int f0, int f4, float f8, float f12, float f16, int f20, int f24, int f28, float f32, int f36, float f40, int f44) : IDummyComponent;
record struct Component48b_1(int f0, float f4, float f8, int f12, float f16, int f20, int f24, float f28, int f32, int f36, int f40, float f44) : IDummyComponent;
record struct Component48b_2(float f0, float f4, float f8, float f12, int f16, float f20, float f24, int f28, float f32, int f36, int f40, int f44) : IDummyComponent;
record struct Component48b_3(float f0, int f4, float f8, float f12, int f16, int f20, float f24, int f28, int f32, float f36, int f40, int f44) : IDummyComponent;
record struct Component48b_4(float f0, float f4, float f8, float f12, float f16, int f20, float f24, float f28, float f32, int f36, float f40, float f44) : IDummyComponent;
record struct Component48b_5(float f0, int f4, int f8, float f12, float f16, float f20, float f24, int f28, float f32, float f36, float f40, float f44) : IDummyComponent;
record struct Component48b_6(float f0, int f4, int f8, int f12, float f16, float f20, int f24, int f28, float f32, int f36, float f40, int f44) : IDummyComponent;
record struct Component48b_7(float f0, int f4, int f8, int f12, int f16, int f20, int f24, int f28, int f32, int f36, float f40, int f44) : IDummyComponent;
record struct Component48b_8(float f0, float f4, int f8, float f12, int f16, float f20, int f24, int f28, float f32, float f36, int f40, int f44) : IDummyComponent;
record struct Component48b_9(float f0, int f4, float f8, float f12, float f16, float f20, int f24, float f28, float f32, int f36, int f40, int f44) : IDummyComponent;
record struct Component48b_10(float f0, int f4, int f8, float f12, int f16, float f20, float f24, float f28, float f32, int f36, float f40, float f44) : IDummyComponent;
record struct Component48b_11(int f0, int f4, float f8, float f12, int f16, int f20, float f24, int f28, int f32, float f36, float f40, int f44) : IDummyComponent;
record struct Component48b_12(float f0, float f4, int f8, int f12, int f16, float f20, float f24, int f28, int f32, float f36, int f40, int f44) : IDummyComponent;
record struct Component48b_13(float f0, int f4, float f8, int f12, float f16, float f20, float f24, int f28, int f32, float f36, float f40, int f44) : IDummyComponent;
record struct Component48b_14(float f0, float f4, float f8, int f12, float f16, float f20, float f24, int f28, int f32, float f36, int f40, float f44) : IDummyComponent;
record struct Component48b_15(int f0, int f4, float f8, float f12, int f16, int f20, int f24, float f28, float f32, float f36, float f40, float f44) : IDummyComponent;
record struct Component48b_16(int f0, float f4, float f8, int f12, float f16, float f20, float f24, float f28, int f32, float f36, float f40, int f44) : IDummyComponent;
record struct Component48b_17(int f0, float f4, float f8, float f12, float f16, int f20, float f24, int f28, int f32, int f36, float f40, float f44) : IDummyComponent;
record struct Component48b_18(int f0, float f4, int f8, float f12, float f16, int f20, int f24, float f28, float f32, int f36, int f40, int f44) : IDummyComponent;
record struct Component48b_19(float f0, float f4, float f8, float f12, int f16, int f20, float f24, float f28, int f32, float f36, int f40, int f44) : IDummyComponent;
record struct Component48b_20(float f0, float f4, int f8, int f12, int f16, int f20, float f24, int f28, int f32, float f36, float f40, int f44) : IDummyComponent;
record struct Component48b_21(int f0, float f4, int f8, float f12, int f16, int f20, float f24, float f28, int f32, int f36, float f40, int f44) : IDummyComponent;
record struct Component52b_0(int f0, int f4, float f8, int f12, float f16, int f20, float f24, float f28, float f32, int f36, int f40, float f44, int f48) : IDummyComponent;
record struct Component52b_1(float f0, float f4, int f8, int f12, int f16, float f20, float f24, float f28, int f32, float f36, int f40, float f44, float f48) : IDummyComponent;
record struct Component52b_2(float f0, float f4, float f8, int f12, int f16, int f20, int f24, int f28, int f32, float f36, int f40, int f44, int f48) : IDummyComponent;
record struct Component52b_3(float f0, int f4, int f8, float f12, float f16, float f20, float f24, int f28, float f32, float f36, int f40, int f44, float f48) : IDummyComponent;
record struct Component52b_4(int f0, float f4, float f8, float f12, int f16, float f20, float f24, float f28, int f32, int f36, float f40, int f44, float f48) : IDummyComponent;
record struct Component52b_5(int f0, int f4, int f8, int f12, int f16, float f20, float f24, float f28, int f32, int f36, int f40, int f44, float f48) : IDummyComponent;
record struct Component52b_6(int f0, float f4, int f8, float f12, float f16, int f20, float f24, float f28, float f32, int f36, int f40, int f44, float f48) : IDummyComponent;
record struct Component52b_7(float f0, float f4, int f8, int f12, int f16, int f20, int f24, int f28, int f32, float f36, int f40, int f44, float f48) : IDummyComponent;
record struct Component52b_8(float f0, float f4, int f8, int f12, int f16, int f20, int f24, float f28, int f32, float f36, int f40, int f44, int f48) : IDummyComponent;
record struct Component52b_9(int f0, int f4, int f8, float f12, float f16, float f20, float f24, int f28, float f32, float f36, int f40, float f44, int f48) : IDummyComponent;
record struct Component52b_10(int f0, float f4, float f8, float f12, float f16, float f20, int f24, int f28, int f32, int f36, int f40, int f44, float f48) : IDummyComponent;
record struct Component52b_11(float f0, int f4, float f8, float f12, float f16, float f20, float f24, int f28, float f32, float f36, float f40, float f44, int f48) : IDummyComponent;
record struct Component52b_12(float f0, int f4, float f8, float f12, float f16, int f20, int f24, int f28, int f32, int f36, int f40, int f44, float f48) : IDummyComponent;
record struct Component52b_13(int f0, float f4, float f8, int f12, float f16, float f20, int f24, int f28, float f32, int f36, int f40, int f44, float f48) : IDummyComponent;
record struct Component52b_14(int f0, int f4, float f8, int f12, int f16, float f20, float f24, int f28, int f32, int f36, int f40, int f44, float f48) : IDummyComponent;
record struct Component52b_15(int f0, float f4, float f8, int f12, int f16, int f20, int f24, float f28, int f32, int f36, int f40, float f44, int f48) : IDummyComponent;
record struct Component52b_16(int f0, float f4, int f8, float f12, float f16, int f20, int f24, int f28, float f32, int f36, float f40, float f44, int f48) : IDummyComponent;
record struct Component52b_17(int f0, int f4, float f8, float f12, int f16, int f20, int f24, float f28, float f32, float f36, int f40, float f44, int f48) : IDummyComponent;
record struct Component52b_18(float f0, int f4, int f8, int f12, int f16, float f20, float f24, int f28, int f32, int f36, int f40, int f44, int f48) : IDummyComponent;
record struct Component52b_19(int f0, int f4, int f8, float f12, int f16, int f20, int f24, int f28, int f32, float f36, float f40, float f44, int f48) : IDummyComponent;
record struct Component52b_20(int f0, float f4, float f8, float f12, float f16, float f20, float f24, int f28, float f32, float f36, float f40, int f44, int f48) : IDummyComponent;
record struct Component56b_0(int f0, int f4, int f8, int f12, int f16, float f20, int f24, int f28, int f32, int f36, int f40, float f44, int f48, float f52) : IDummyComponent;
record struct Component56b_1(float f0, int f4, int f8, int f12, float f16, float f20, float f24, float f28, float f32, int f36, float f40, int f44, int f48, float f52) : IDummyComponent;
record struct Component56b_2(int f0, int f4, float f8, float f12, float f16, int f20, int f24, int f28, float f32, float f36, float f40, int f44, int f48, float f52) : IDummyComponent;
record struct Component56b_3(float f0, float f4, int f8, float f12, float f16, int f20, float f24, int f28, float f32, float f36, int f40, float f44, float f48, float f52) : IDummyComponent;
record struct Component56b_4(int f0, float f4, int f8, int f12, float f16, int f20, int f24, int f28, float f32, float f36, float f40, float f44, int f48, int f52) : IDummyComponent;
record struct Component56b_5(int f0, int f4, float f8, int f12, int f16, int f20, float f24, int f28, int f32, int f36, int f40, int f44, float f48, int f52) : IDummyComponent;
record struct Component56b_6(int f0, int f4, int f8, float f12, float f16, float f20, float f24, int f28, int f32, float f36, float f40, float f44, float f48, int f52) : IDummyComponent;
record struct Component56b_7(int f0, int f4, int f8, float f12, int f16, int f20, int f24, float f28, float f32, float f36, float f40, float f44, float f48, int f52) : IDummyComponent;
record struct Component56b_8(int f0, int f4, float f8, int f12, int f16, int f20, int f24, float f28, float f32, int f36, int f40, int f44, int f48, float f52) : IDummyComponent;
record struct Component56b_9(int f0, float f4, float f8, int f12, float f16, float f20, int f24, float f28, float f32, int f36, float f40, float f44, int f48, float f52) : IDummyComponent;
record struct Component56b_10(int f0, float f4, int f8, float f12, int f16, float f20, float f24, float f28, int f32, float f36, float f40, float f44, int f48, float f52) : IDummyComponent;
record struct Component56b_11(int f0, float f4, float f8, float f12, float f16, int f20, int f24, float f28, float f32, int f36, float f40, float f44, float f48, int f52) : IDummyComponent;
record struct Component56b_12(int f0, float f4, int f8, int f12, int f16, float f20, float f24, int f28, int f32, float f36, int f40, int f44, float f48, int f52) : IDummyComponent;
record struct Component56b_13(int f0, int f4, int f8, int f12, float f16, int f20, float f24, float f28, float f32, float f36, float f40, float f44, float f48, float f52) : IDummyComponent;
record struct Component56b_14(int f0, float f4, int f8, int f12, float f16, int f20, int f24, int f28, float f32, float f36, int f40, float f44, float f48, int f52) : IDummyComponent;
record struct Component56b_15(int f0, float f4, int f8, float f12, int f16, float f20, int f24, float f28, float f32, int f36, float f40, float f44, float f48, float f52) : IDummyComponent;
record struct Component56b_16(float f0, float f4, float f8, int f12, int f16, int f20, int f24, int f28, int f32, float f36, int f40, float f44, int f48, int f52) : IDummyComponent;
record struct Component56b_17(int f0, int f4, float f8, int f12, float f16, int f20, float f24, float f28, float f32, float f36, int f40, float f44, float f48, int f52) : IDummyComponent;
record struct Component56b_18(float f0, int f4, float f8, float f12, float f16, float f20, float f24, int f28, int f32, float f36, int f40, int f44, int f48, int f52) : IDummyComponent;
record struct Component60b_0(float f0, float f4, int f8, int f12, int f16, int f20, float f24, int f28, float f32, int f36, int f40, float f44, int f48, int f52, int f56) : IDummyComponent;
record struct Component60b_1(int f0, float f4, int f8, int f12, int f16, int f20, int f24, int f28, float f32, int f36, int f40, float f44, int f48, int f52, int f56) : IDummyComponent;
record struct Component60b_2(int f0, float f4, float f8, float f12, int f16, float f20, float f24, float f28, int f32, float f36, float f40, int f44, int f48, float f52, float f56) : IDummyComponent;
record struct Component60b_3(float f0, int f4, int f8, int f12, int f16, int f20, float f24, float f28, float f32, int f36, int f40, int f44, int f48, float f52, float f56) : IDummyComponent;
record struct Component60b_4(float f0, int f4, int f8, int f12, float f16, int f20, float f24, float f28, float f32, float f36, int f40, int f44, float f48, float f52, int f56) : IDummyComponent;
record struct Component60b_5(int f0, int f4, float f8, int f12, int f16, float f20, float f24, float f28, int f32, int f36, float f40, float f44, float f48, float f52, int f56) : IDummyComponent;
record struct Component60b_6(float f0, int f4, int f8, int f12, float f16, int f20, int f24, int f28, int f32, int f36, int f40, float f44, float f48, int f52, float f56) : IDummyComponent;
record struct Component60b_7(float f0, float f4, float f8, float f12, int f16, float f20, float f24, int f28, int f32, float f36, float f40, int f44, int f48, float f52, int f56) : IDummyComponent;
record struct Component60b_8(float f0, float f4, float f8, float f12, float f16, float f20, float f24, int f28, float f32, float f36, int f40, float f44, float f48, int f52, int f56) : IDummyComponent;
record struct Component60b_9(float f0, float f4, int f8, int f12, float f16, float f20, int f24, int f28, int f32, int f36, int f40, float f44, int f48, int f52, int f56) : IDummyComponent;
record struct Component60b_10(float f0, float f4, float f8, float f12, float f16, float f20, float f24, float f28, float f32, int f36, float f40, int f44, float f48, int f52, int f56) : IDummyComponent;
record struct Component60b_11(int f0, float f4, float f8, float f12, int f16, int f20, int f24, float f28, float f32, int f36, float f40, int f44, int f48, int f52, float f56) : IDummyComponent;
record struct Component60b_12(int f0, float f4, float f8, int f12, int f16, int f20, float f24, int f28, float f32, float f36, float f40, float f44, int f48, int f52, float f56) : IDummyComponent;
record struct Component60b_13(float f0, float f4, float f8, int f12, int f16, float f20, float f24, int f28, float f32, int f36, float f40, int f44, float f48, float f52, float f56) : IDummyComponent;
record struct Component60b_14(int f0, int f4, int f8, int f12, int f16, int f20, float f24, float f28, int f32, float f36, float f40, float f44, float f48, float f52, int f56) : IDummyComponent;
record struct Component60b_15(float f0, int f4, float f8, int f12, float f16, float f20, int f24, int f28, float f32, int f36, int f40, int f44, int f48, int f52, int f56) : IDummyComponent;
record struct Component60b_16(int f0, float f4, float f8, float f12, int f16, int f20, float f24, int f28, int f32, float f36, float f40, int f44, int f48, float f52, int f56) : IDummyComponent;
record struct Component60b_17(float f0, float f4, int f8, float f12, float f16, float f20, float f24, float f28, int f32, float f36, int f40, float f44, float f48, int f52, float f56) : IDummyComponent;
record struct Component64b_0(float f0, int f4, int f8, int f12, float f16, int f20, float f24, float f28, float f32, int f36, int f40, float f44, int f48, int f52, float f56, int f60) : IDummyComponent;
record struct Component64b_1(int f0, int f4, float f8, float f12, float f16, float f20, int f24, int f28, float f32, int f36, float f40, int f44, float f48, float f52, int f56, float f60) : IDummyComponent;
record struct Component64b_2(float f0, int f4, int f8, float f12, int f16, int f20, float f24, float f28, int f32, int f36, float f40, int f44, float f48, float f52, int f56, int f60) : IDummyComponent;
record struct Component64b_3(float f0, int f4, int f8, int f12, int f16, float f20, int f24, float f28, float f32, float f36, float f40, float f44, float f48, float f52, int f56, float f60) : IDummyComponent;
record struct Component64b_4(float f0, float f4, int f8, int f12, float f16, float f20, int f24, float f28, int f32, int f36, int f40, int f44, int f48, int f52, int f56, int f60) : IDummyComponent;
record struct Component64b_5(float f0, float f4, float f8, int f12, float f16, int f20, float f24, float f28, int f32, float f36, int f40, float f44, float f48, int f52, int f56, int f60) : IDummyComponent;
record struct Component64b_6(float f0, float f4, int f8, float f12, float f16, float f20, int f24, float f28, int f32, float f36, float f40, float f44, float f48, int f52, float f56, float f60) : IDummyComponent;
record struct Component64b_7(int f0, float f4, float f8, float f12, float f16, float f20, int f24, int f28, int f32, float f36, float f40, int f44, float f48, float f52, int f56, int f60) : IDummyComponent;
record struct Component64b_8(float f0, int f4, float f8, float f12, float f16, float f20, float f24, float f28, int f32, float f36, float f40, int f44, float f48, int f52, float f56, int f60) : IDummyComponent;
record struct Component64b_9(float f0, float f4, float f8, int f12, int f16, int f20, int f24, int f28, float f32, int f36, float f40, float f44, float f48, int f52, float f56, float f60) : IDummyComponent;
record struct Component64b_10(float f0, int f4, float f8, float f12, float f16, float f20, float f24, int f28, float f32, int f36, float f40, int f44, int f48, int f52, int f56, int f60) : IDummyComponent;
record struct Component64b_11(float f0, float f4, int f8, int f12, float f16, float f20, int f24, float f28, int f32, int f36, float f40, int f44, float f48, int f52, int f56, float f60) : IDummyComponent;
record struct Component64b_12(float f0, int f4, float f8, int f12, int f16, float f20, float f24, float f28, int f32, float f36, float f40, int f44, int f48, float f52, float f56, int f60) : IDummyComponent;
record struct Component64b_13(int f0, int f4, float f8, int f12, int f16, float f20, int f24, float f28, float f32, int f36, int f40, int f44, float f48, int f52, int f56, int f60) : IDummyComponent;
record struct Component64b_14(int f0, int f4, float f8, float f12, int f16, float f20, float f24, float f28, int f32, float f36, float f40, float f44, int f48, float f52, int f56, int f60) : IDummyComponent;
record struct Component64b_15(float f0, int f4, float f8, float f12, int f16, int f20, int f24, float f28, float f32, int f36, int f40, int f44, int f48, int f52, float f56, int f60) : IDummyComponent;
record struct Component68b_0(int f0, int f4, int f8, float f12, int f16, float f20, int f24, int f28, int f32, int f36, int f40, int f44, int f48, float f52, float f56, int f60, int f64) : IDummyComponent;
record struct Component68b_1(float f0, float f4, int f8, float f12, int f16, int f20, int f24, float f28, float f32, float f36, float f40, float f44, int f48, float f52, float f56, float f60, float f64) : IDummyComponent;
record struct Component68b_2(int f0, int f4, float f8, float f12, int f16, int f20, int f24, int f28, int f32, int f36, float f40, float f44, float f48, int f52, float f56, float f60, float f64) : IDummyComponent;
record struct Component68b_3(float f0, float f4, int f8, int f12, float f16, float f20, float f24, float f28, float f32, float f36, int f40, int f44, int f48, int f52, int f56, int f60, int f64) : IDummyComponent;
record struct Component68b_4(float f0, float f4, int f8, float f12, int f16, float f20, float f24, int f28, float f32, int f36, int f40, float f44, float f48, int f52, int f56, float f60, int f64) : IDummyComponent;
record struct Component68b_5(float f0, float f4, int f8, float f12, float f16, float f20, float f24, int f28, int f32, float f36, int f40, float f44, float f48, float f52, int f56, float f60, int f64) : IDummyComponent;
record struct Component68b_6(float f0, int f4, float f8, int f12, int f16, int f20, int f24, int f28, int f32, int f36, int f40, int f44, int f48, float f52, int f56, int f60, float f64) : IDummyComponent;
record struct Component68b_7(float f0, float f4, int f8, int f12, float f16, float f20, float f24, float f28, int f32, int f36, int f40, int f44, float f48, int f52, float f56, float f60, int f64) : IDummyComponent;
record struct Component68b_8(float f0, int f4, float f8, float f12, int f16, float f20, float f24, float f28, float f32, int f36, int f40, int f44, float f48, float f52, float f56, int f60, float f64) : IDummyComponent;
record struct Component68b_9(int f0, float f4, int f8, float f12, float f16, float f20, float f24, float f28, float f32, int f36, float f40, float f44, float f48, float f52, float f56, float f60, float f64) : IDummyComponent;
record struct Component68b_10(int f0, float f4, float f8, float f12, int f16, float f20, float f24, int f28, float f32, int f36, float f40, int f44, float f48, int f52, float f56, int f60, int f64) : IDummyComponent;
record struct Component68b_11(int f0, float f4, int f8, int f12, int f16, float f20, int f24, int f28, int f32, int f36, int f40, int f44, int f48, int f52, int f56, int f60, float f64) : IDummyComponent;
record struct Component68b_12(float f0, int f4, float f8, int f12, int f16, int f20, float f24, float f28, float f32, float f36, float f40, int f44, int f48, float f52, int f56, float f60, int f64) : IDummyComponent;
record struct Component68b_13(float f0, int f4, float f8, float f12, float f16, float f20, float f24, int f28, float f32, int f36, float f40, float f44, int f48, int f52, float f56, float f60, float f64) : IDummyComponent;
record struct Component68b_14(float f0, float f4, int f8, float f12, float f16, float f20, float f24, float f28, float f32, int f36, float f40, int f44, int f48, float f52, int f56, int f60, int f64) : IDummyComponent;
record struct Component72b_0(int f0, float f4, float f8, int f12, float f16, int f20, float f24, float f28, float f32, int f36, int f40, float f44, int f48, int f52, int f56, float f60, int f64, float f68) : IDummyComponent;
record struct Component72b_1(int f0, int f4, int f8, float f12, float f16, float f20, int f24, float f28, float f32, int f36, float f40, float f44, int f48, int f52, float f56, float f60, int f64, float f68) : IDummyComponent;
record struct Component72b_2(int f0, float f4, float f8, float f12, int f16, int f20, int f24, int f28, int f32, int f36, int f40, float f44, int f48, float f52, int f56, float f60, float f64, float f68) : IDummyComponent;
record struct Component72b_3(float f0, float f4, float f8, int f12, float f16, int f20, float f24, int f28, float f32, float f36, int f40, int f44, int f48, float f52, int f56, int f60, float f64, int f68) : IDummyComponent;
record struct Component72b_4(int f0, int f4, float f8, int f12, int f16, int f20, float f24, int f28, int f32, int f36, float f40, int f44, float f48, int f52, int f56, int f60, int f64, int f68) : IDummyComponent;
record struct Component72b_5(float f0, float f4, float f8, int f12, float f16, int f20, float f24, float f28, int f32, float f36, int f40, float f44, int f48, float f52, int f56, int f60, int f64, int f68) : IDummyComponent;
record struct Component72b_6(float f0, float f4, int f8, int f12, float f16, float f20, int f24, int f28, int f32, float f36, float f40, int f44, int f48, float f52, float f56, int f60, int f64, int f68) : IDummyComponent;
record struct Component72b_7(int f0, float f4, float f8, float f12, float f16, int f20, int f24, float f28, float f32, float f36, int f40, int f44, int f48, int f52, int f56, float f60, int f64, float f68) : IDummyComponent;
record struct Component72b_8(float f0, float f4, float f8, float f12, float f16, int f20, float f24, int f28, int f32, float f36, float f40, float f44, float f48, int f52, int f56, float f60, int f64, float f68) : IDummyComponent;
record struct Component72b_9(float f0, float f4, int f8, float f12, int f16, int f20, float f24, float f28, int f32, int f36, float f40, float f44, float f48, float f52, int f56, float f60, int f64, int f68) : IDummyComponent;
record struct Component72b_10(float f0, float f4, float f8, int f12, int f16, float f20, int f24, int f28, int f32, float f36, float f40, int f44, float f48, int f52, float f56, int f60, int f64, int f68) : IDummyComponent;
record struct Component72b_11(float f0, int f4, float f8, int f12, int f16, int f20, int f24, int f28, int f32, float f36, float f40, int f44, float f48, int f52, int f56, int f60, int f64, int f68) : IDummyComponent;
record struct Component72b_12(int f0, int f4, float f8, int f12, int f16, int f20, int f24, int f28, float f32, int f36, float f40, int f44, int f48, int f52, float f56, float f60, float f64, float f68) : IDummyComponent;
record struct Component72b_13(int f0, float f4, int f8, float f12, float f16, float f20, int f24, float f28, float f32, float f36, float f40, int f44, float f48, float f52, float f56, int f60, float f64, float f68) : IDummyComponent;
record struct Component76b_0(int f0, int f4, float f8, int f12, int f16, int f20, int f24, int f28, int f32, float f36, int f40, int f44, int f48, int f52, int f56, float f60, float f64, int f68, int f72) : IDummyComponent;
record struct Component76b_1(float f0, int f4, float f8, int f12, float f16, float f20, float f24, float f28, float f32, int f36, int f40, int f44, float f48, float f52, float f56, float f60, int f64, float f68, float f72) : IDummyComponent;
record struct Component76b_2(int f0, float f4, int f8, int f12, float f16, int f20, int f24, float f28, float f32, int f36, int f40, float f44, float f48, int f52, float f56, int f60, int f64, float f68, float f72) : IDummyComponent;
record struct Component76b_3(int f0, float f4, int f8, int f12, int f16, float f20, int f24, int f28, float f32, float f36, float f40, float f44, float f48, int f52, int f56, int f60, float f64, float f68, int f72) : IDummyComponent;
record struct Component76b_4(float f0, float f4, int f8, float f12, float f16, int f20, float f24, int f28, float f32, float f36, int f40, int f44, int f48, int f52, float f56, float f60, int f64, float f68, float f72) : IDummyComponent;
record struct Component76b_5(int f0, int f4, int f8, float f12, int f16, float f20, int f24, float f28, float f32, float f36, float f40, int f44, float f48, float f52, int f56, float f60, int f64, int f68, float f72) : IDummyComponent;
record struct Component76b_6(int f0, float f4, float f8, int f12, float f16, float f20, float f24, int f28, float f32, float f36, float f40, float f44, float f48, int f52, int f56, float f60, int f64, float f68, int f72) : IDummyComponent;
record struct Component76b_7(int f0, int f4, int f8, int f12, int f16, float f20, int f24, int f28, int f32, float f36, int f40, float f44, float f48, int f52, float f56, int f60, float f64, int f68, float f72) : IDummyComponent;
record struct Component76b_8(float f0, int f4, float f8, float f12, int f16, int f20, int f24, int f28, int f32, int f36, float f40, float f44, int f48, int f52, int f56, float f60, int f64, int f68, float f72) : IDummyComponent;
record struct Component76b_9(float f0, float f4, int f8, int f12, int f16, int f20, float f24, int f28, float f32, float f36, float f40, float f44, int f48, float f52, int f56, float f60, int f64, float f68, int f72) : IDummyComponent;
record struct Component76b_10(float f0, float f4, float f8, int f12, int f16, int f20, float f24, float f28, int f32, float f36, int f40, float f44, int f48, int f52, float f56, int f60, int f64, int f68, float f72) : IDummyComponent;
record struct Component76b_11(int f0, float f4, float f8, float f12, int f16, float f20, float f24, int f28, int f32, float f36, int f40, float f44, float f48, float f52, int f56, int f60, int f64, int f68, int f72) : IDummyComponent;
record struct Component76b_12(int f0, float f4, float f8, int f12, int f16, float f20, int f24, float f28, int f32, int f36, float f40, int f44, float f48, float f52, float f56, int f60, float f64, int f68, int f72) : IDummyComponent;
record struct Component76b_13(int f0, int f4, float f8, int f12, float f16, int f20, int f24, float f28, float f32, int f36, float f40, int f44, int f48, int f52, float f56, int f60, float f64, int f68, int f72) : IDummyComponent;
record struct Component80b_0(float f0, int f4, float f8, int f12, float f16, float f20, float f24, float f28, int f32, int f36, int f40, float f44, float f48, int f52, float f56, float f60, int f64, int f68, float f72, int f76) : IDummyComponent;
record struct Component80b_1(float f0, int f4, int f8, float f12, float f16, float f20, int f24, int f28, int f32, float f36, float f40, float f44, int f48, int f52, float f56, float f60, float f64, int f68, float f72, int f76) : IDummyComponent;
record struct Component80b_2(float f0, int f4, int f8, int f12, int f16, float f20, int f24, float f28, int f32, float f36, int f40, float f44, float f48, int f52, int f56, int f60, int f64, float f68, float f72, int f76) : IDummyComponent;
record struct Component80b_3(int f0, float f4, float f8, int f12, float f16, int f20, float f24, int f28, int f32, float f36, float f40, float f44, float f48, int f52, float f56, float f60, int f64, int f68, int f72, int f76) : IDummyComponent;
record struct Component80b_4(int f0, int f4, float f8, float f12, int f16, float f20, int f24, float f28, float f32, float f36, float f40, int f44, int f48, int f52, float f56, float f60, int f64, int f68, int f72, int f76) : IDummyComponent;
record struct Component80b_5(int f0, float f4, float f8, int f12, int f16, float f20, float f24, float f28, int f32, int f36, int f40, float f44, int f48, float f52, int f56, int f60, float f64, float f68, float f72, int f76) : IDummyComponent;
record struct Component80b_6(float f0, float f4, float f8, int f12, float f16, int f20, int f24, float f28, float f32, int f36, int f40, int f44, int f48, int f52, float f56, int f60, float f64, float f68, int f72, int f76) : IDummyComponent;
record struct Component80b_7(float f0, float f4, int f8, float f12, int f16, int f20, int f24, int f28, float f32, float f36, int f40, int f44, int f48, int f52, float f56, float f60, int f64, float f68, int f72, float f76) : IDummyComponent;
record struct Component80b_8(int f0, float f4, int f8, float f12, float f16, int f20, int f24, float f28, float f32, int f36, float f40, int f44, float f48, float f52, int f56, float f60, int f64, float f68, float f72, float f76) : IDummyComponent;
record struct Component80b_9(float f0, float f4, float f8, float f12, float f16, float f20, float f24, float f28, float f32, int f36, int f40, float f44, float f48, int f52, int f56, float f60, int f64, int f68, int f72, int f76) : IDummyComponent;
record struct Component80b_10(float f0, float f4, float f8, int f12, float f16, int f20, float f24, float f28, float f32, int f36, float f40, float f44, float f48, float f52, float f56, int f60, int f64, float f68, int f72, int f76) : IDummyComponent;
record struct Component80b_11(int f0, float f4, float f8, float f12, int f16, int f20, int f24, float f28, float f32, float f36, int f40, int f44, int f48, float f52, int f56, int f60, float f64, float f68, int f72, float f76) : IDummyComponent;
record struct Component80b_12(int f0, int f4, int f8, float f12, float f16, int f20, float f24, int f28, float f32, int f36, float f40, float f44, int f48, int f52, int f56, float f60, float f64, int f68, float f72, float f76) : IDummyComponent;
record struct Component84b_0(int f0, int f4, float f8, float f12, float f16, float f20, float f24, float f28, int f32, int f36, int f40, int f44, int f48, int f52, float f56, float f60, int f64, int f68, float f72, int f76, float f80) : IDummyComponent;
record struct Component84b_1(int f0, int f4, float f8, float f12, int f16, float f20, int f24, int f28, float f32, int f36, float f40, int f44, int f48, float f52, float f56, float f60, float f64, int f68, int f72, float f76, int f80) : IDummyComponent;
record struct Component84b_2(int f0, int f4, float f8, int f12, int f16, int f20, float f24, int f28, float f32, int f36, int f40, int f44, float f48, int f52, int f56, float f60, float f64, float f68, float f72, int f76, int f80) : IDummyComponent;
record struct Component84b_3(float f0, int f4, int f8, int f12, float f16, int f20, float f24, int f28, int f32, int f36, int f40, float f44, int f48, int f52, int f56, int f60, int f64, float f68, float f72, float f76, float f80) : IDummyComponent;
record struct Component84b_4(float f0, int f4, float f8, int f12, int f16, float f20, float f24, float f28, int f32, int f36, int f40, int f44, int f48, int f52, int f56, float f60, float f64, int f68, float f72, float f76, float f80) : IDummyComponent;
record struct Component84b_5(int f0, float f4, int f8, float f12, int f16, int f20, int f24, int f28, int f32, int f36, int f40, int f44, float f48, int f52, float f56, float f60, float f64, int f68, int f72, float f76, int f80) : IDummyComponent;
record struct Component84b_6(int f0, int f4, int f8, float f12, int f16, int f20, float f24, float f28, float f32, int f36, int f40, int f44, float f48, float f52, int f56, float f60, int f64, float f68, int f72, int f76, float f80) : IDummyComponent;
record struct Component84b_7(int f0, int f4, int f8, int f12, int f16, int f20, int f24, int f28, int f32, int f36, int f40, int f44, int f48, int f52, int f56, float f60, float f64, int f68, int f72, int f76, float f80) : IDummyComponent;
record struct Component84b_8(float f0, float f4, int f8, int f12, float f16, int f20, float f24, float f28, int f32, float f36, int f40, float f44, int f48, int f52, float f56, float f60, int f64, float f68, int f72, int f76, int f80) : IDummyComponent;
record struct Component84b_9(int f0, int f4, float f8, int f12, float f16, int f20, float f24, int f28, float f32, int f36, int f40, float f44, int f48, float f52, int f56, float f60, float f64, float f68, int f72, float f76, int f80) : IDummyComponent;
record struct Component84b_10(float f0, int f4, float f8, float f12, float f16, float f20, float f24, float f28, float f32, int f36, int f40, float f44, int f48, int f52, int f56, float f60, float f64, float f68, int f72, int f76, float f80) : IDummyComponent;
record struct Component84b_11(int f0, float f4, float f8, int f12, int f16, float f20, int f24, float f28, float f32, int f36, int f40, float f44, int f48, float f52, float f56, float f60, int f64, float f68, float f72, int f76, float f80) : IDummyComponent;
record struct Component88b_0(int f0, int f4, int f8, int f12, float f16, float f20, float f24, float f28, int f32, int f36, int f40, float f44, int f48, float f52, float f56, int f60, float f64, float f68, float f72, float f76, int f80, float f84) : IDummyComponent;
record struct Component88b_1(float f0, int f4, float f8, int f12, int f16, int f20, int f24, int f28, int f32, float f36, float f40, float f44, float f48, int f52, float f56, int f60, float f64, float f68, float f72, float f76, float f80, float f84) : IDummyComponent;
record struct Component88b_2(float f0, int f4, float f8, int f12, int f16, float f20, float f24, int f28, int f32, float f36, int f40, float f44, float f48, float f52, float f56, float f60, int f64, int f68, float f72, int f76, float f80, int f84) : IDummyComponent;
record struct Component88b_3(float f0, float f4, float f8, float f12, int f16, int f20, int f24, int f28, float f32, int f36, float f40, int f44, int f48, int f52, float f56, int f60, int f64, float f68, float f72, float f76, int f80, int f84) : IDummyComponent;
record struct Component88b_4(int f0, int f4, int f8, float f12, float f16, float f20, float f24, float f28, float f32, float f36, int f40, float f44, int f48, float f52, float f56, float f60, float f64, int f68, int f72, float f76, float f80, int f84) : IDummyComponent;
record struct Component88b_5(float f0, int f4, float f8, float f12, int f16, int f20, int f24, int f28, int f32, float f36, float f40, int f44, int f48, int f52, int f56, int f60, int f64, float f68, int f72, float f76, float f80, float f84) : IDummyComponent;
record struct Component88b_6(int f0, int f4, int f8, int f12, float f16, int f20, int f24, float f28, int f32, int f36, int f40, float f44, float f48, float f52, int f56, int f60, float f64, int f68, int f72, int f76, int f80, int f84) : IDummyComponent;
record struct Component88b_7(float f0, float f4, int f8, float f12, int f16, float f20, int f24, int f28, float f32, int f36, int f40, int f44, int f48, int f52, int f56, int f60, float f64, int f68, float f72, int f76, float f80, int f84) : IDummyComponent;
record struct Component88b_8(int f0, int f4, float f8, int f12, float f16, int f20, float f24, int f28, float f32, float f36, int f40, int f44, int f48, float f52, float f56, float f60, int f64, float f68, float f72, int f76, float f80, float f84) : IDummyComponent;
record struct Component88b_9(float f0, int f4, int f8, int f12, int f16, int f20, int f24, int f28, int f32, int f36, float f40, int f44, int f48, float f52, int f56, int f60, int f64, float f68, int f72, int f76, float f80, int f84) : IDummyComponent;
record struct Component88b_10(float f0, float f4, float f8, int f12, float f16, int f20, float f24, float f28, float f32, int f36, int f40, int f44, int f48, int f52, float f56, float f60, int f64, int f68, float f72, int f76, int f80, int f84) : IDummyComponent;
record struct Component88b_11(int f0, int f4, float f8, float f12, float f16, float f20, float f24, float f28, float f32, int f36, int f40, float f44, int f48, int f52, int f56, float f60, float f64, float f68, int f72, int f76, float f80, int f84) : IDummyComponent;
record struct Component92b_0(float f0, int f4, float f8, int f12, int f16, float f20, float f24, int f28, float f32, float f36, int f40, float f44, float f48, int f52, int f56, float f60, int f64, int f68, float f72, float f76, float f80, float f84, int f88) : IDummyComponent;
record struct Component92b_1(int f0, int f4, int f8, int f12, float f16, int f20, int f24, float f28, int f32, float f36, int f40, float f44, float f48, int f52, float f56, float f60, float f64, int f68, int f72, int f76, float f80, int f84, int f88) : IDummyComponent;
record struct Component92b_2(int f0, float f4, float f8, int f12, int f16, float f20, float f24, float f28, int f32, float f36, float f40, int f44, int f48, float f52, int f56, float f60, float f64, float f68, float f72, float f76, float f80, int f84, float f88) : IDummyComponent;
record struct Component92b_3(int f0, int f4, float f8, int f12, int f16, float f20, int f24, float f28, int f32, float f36, float f40, int f44, int f48, int f52, float f56, float f60, float f64, int f68, float f72, int f76, float f80, float f84, int f88) : IDummyComponent;
record struct Component92b_4(int f0, float f4, float f8, float f12, float f16, float f20, int f24, float f28, int f32, float f36, int f40, int f44, int f48, int f52, int f56, float f60, float f64, int f68, int f72, int f76, float f80, float f84, int f88) : IDummyComponent;
record struct Component92b_5(int f0, float f4, int f8, float f12, int f16, float f20, int f24, int f28, float f32, float f36, float f40, int f44, float f48, float f52, int f56, int f60, float f64, int f68, float f72, float f76, int f80, float f84, float f88) : IDummyComponent;
record struct Component92b_6(int f0, float f4, int f8, float f12, float f16, int f20, float f24, int f28, float f32, int f36, int f40, float f44, int f48, float f52, float f56, float f60, int f64, float f68, float f72, int f76, int f80, int f84, float f88) : IDummyComponent;
record struct Component92b_7(int f0, float f4, float f8, float f12, float f16, int f20, float f24, int f28, float f32, float f36, int f40, float f44, float f48, float f52, float f56, float f60, float f64, float f68, int f72, float f76, float f80, float f84, float f88) : IDummyComponent;
record struct Component92b_8(float f0, float f4, int f8, int f12, int f16, int f20, float f24, float f28, float f32, int f36, int f40, float f44, float f48, float f52, int f56, int f60, int f64, int f68, float f72, int f76, float f80, float f84, float f88) : IDummyComponent;
record struct Component92b_9(int f0, int f4, float f8, float f12, int f16, float f20, int f24, float f28, int f32, float f36, int f40, float f44, int f48, int f52, int f56, int f60, int f64, float f68, float f72, int f76, float f80, int f84, float f88) : IDummyComponent;
record struct Component92b_10(int f0, int f4, float f8, float f12, int f16, int f20, int f24, int f28, int f32, int f36, float f40, int f44, int f48, int f52, int f56, float f60, int f64, int f68, float f72, int f76, float f80, int f84, float f88) : IDummyComponent;
record struct Component96b_0(float f0, int f4, int f8, float f12, float f16, float f20, int f24, int f28, int f32, int f36, int f40, int f44, int f48, float f52, float f56, float f60, int f64, float f68, int f72, int f76, int f80, float f84, float f88, int f92) : IDummyComponent;
record struct Component96b_1(float f0, int f4, int f8, float f12, int f16, float f20, int f24, int f28, float f32, float f36, float f40, float f44, int f48, float f52, float f56, float f60, int f64, float f68, int f72, int f76, float f80, float f84, float f88, int f92) : IDummyComponent;
record struct Component96b_2(float f0, int f4, int f8, float f12, float f16, int f20, int f24, float f28, float f32, int f36, int f40, float f44, float f48, float f52, float f56, float f60, float f64, float f68, float f72, int f76, int f80, float f84, int f88, float f92) : IDummyComponent;
record struct Component96b_3(int f0, float f4, int f8, float f12, float f16, int f20, float f24, int f28, int f32, int f36, int f40, float f44, int f48, float f52, int f56, float f60, int f64, int f68, int f72, int f76, float f80, float f84, int f88, float f92) : IDummyComponent;
record struct Component96b_4(int f0, float f4, float f8, int f12, int f16, int f20, float f24, float f28, float f32, float f36, int f40, int f44, int f48, int f52, int f56, float f60, int f64, float f68, float f72, float f76, float f80, float f84, float f88, int f92) : IDummyComponent;
record struct Component96b_5(float f0, float f4, int f8, float f12, float f16, float f20, float f24, float f28, int f32, float f36, float f40, int f44, float f48, float f52, int f56, float f60, int f64, float f68, float f72, int f76, float f80, int f84, int f88, int f92) : IDummyComponent;
record struct Component96b_6(float f0, int f4, float f8, int f12, int f16, int f20, float f24, int f28, int f32, float f36, float f40, float f44, float f48, int f52, float f56, int f60, int f64, float f68, float f72, float f76, int f80, int f84, float f88, int f92) : IDummyComponent;
record struct Component96b_7(int f0, float f4, int f8, float f12, int f16, float f20, int f24, float f28, float f32, float f36, int f40, int f44, float f48, int f52, int f56, float f60, int f64, int f68, float f72, float f76, float f80, float f84, int f88, float f92) : IDummyComponent;
record struct Component96b_8(int f0, int f4, int f8, float f12, int f16, float f20, int f24, float f28, float f32, int f36, int f40, float f44, float f48, float f52, float f56, float f60, float f64, float f68, int f72, float f76, int f80, int f84, float f88, float f92) : IDummyComponent;
record struct Component96b_9(int f0, float f4, float f8, float f12, int f16, float f20, int f24, int f28, int f32, int f36, float f40, int f44, int f48, float f52, int f56, int f60, int f64, int f68, int f72, float f76, float f80, float f84, float f88, float f92) : IDummyComponent;
record struct Component100b_0(float f0, int f4, int f8, int f12, int f16, float f20, int f24, float f28, float f32, int f36, float f40, float f44, int f48, int f52, float f56, int f60, float f64, float f68, float f72, int f76, int f80, float f84, float f88, int f92, float f96) : IDummyComponent;
record struct Component100b_1(float f0, int f4, float f8, int f12, int f16, float f20, int f24, int f28, float f32, int f36, int f40, float f44, int f48, int f52, float f56, int f60, float f64, int f68, float f72, int f76, int f80, int f84, int f88, float f92, int f96) : IDummyComponent;
record struct Component100b_2(int f0, int f4, float f8, int f12, int f16, int f20, float f24, float f28, float f32, int f36, int f40, int f44, float f48, int f52, int f56, float f60, int f64, float f68, int f72, int f76, int f80, int f84, float f88, float f92, float f96) : IDummyComponent;
record struct Component100b_3(float f0, float f4, float f8, float f12, int f16, int f20, float f24, int f28, int f32, float f36, float f40, int f44, int f48, int f52, float f56, float f60, int f64, int f68, float f72, int f76, float f80, int f84, int f88, int f92, float f96) : IDummyComponent;
record struct Component100b_4(int f0, int f4, float f8, float f12, float f16, float f20, int f24, float f28, int f32, float f36, int f40, int f44, float f48, float f52, float f56, int f60, float f64, int f68, float f72, float f76, float f80, int f84, int f88, float f92, float f96) : IDummyComponent;
record struct Component100b_5(float f0, float f4, float f8, int f12, float f16, float f20, float f24, int f28, int f32, float f36, float f40, float f44, float f48, float f52, int f56, int f60, int f64, float f68, int f72, float f76, int f80, int f84, float f88, float f92, float f96) : IDummyComponent;
record struct Component100b_6(int f0, float f4, float f8, int f12, float f16, int f20, float f24, float f28, float f32, int f36, float f40, float f44, float f48, float f52, int f56, int f60, int f64, int f68, int f72, int f76, float f80, int f84, int f88, int f92, int f96) : IDummyComponent;
record struct Component100b_7(float f0, float f4, int f8, float f12, int f16, int f20, int f24, float f28, float f32, float f36, float f40, float f44, int f48, float f52, float f56, int f60, int f64, float f68, float f72, int f76, int f80, float f84, int f88, int f92, int f96) : IDummyComponent;
record struct Component100b_8(float f0, float f4, int f8, int f12, int f16, int f20, int f24, float f28, float f32, int f36, float f40, int f44, int f48, int f52, int f56, int f60, float f64, int f68, float f72, float f76, float f80, float f84, float f88, float f92, float f96) : IDummyComponent;
record struct Component100b_9(int f0, int f4, float f8, int f12, int f16, float f20, float f24, int f28, float f32, float f36, int f40, int f44, int f48, int f52, float f56, float f60, int f64, int f68, int f72, int f76, float f80, int f84, int f88, int f92, float f96) : IDummyComponent;
record struct Component104b_0(int f0, float f4, int f8, float f12, int f16, int f20, float f24, int f28, float f32, float f36, int f40, float f44, float f48, int f52, int f56, int f60, float f64, int f68, int f72, float f76, float f80, float f84, float f88, float f92, float f96, float f100) : IDummyComponent;
record struct Component104b_1(float f0, float f4, float f8, float f12, float f16, float f20, int f24, float f28, int f32, float f36, float f40, int f44, int f48, int f52, int f56, int f60, int f64, int f68, int f72, float f76, float f80, float f84, float f88, float f92, int f96, int f100) : IDummyComponent;
record struct Component104b_2(float f0, int f4, float f8, float f12, int f16, int f20, int f24, int f28, int f32, int f36, int f40, float f44, float f48, float f52, float f56, int f60, int f64, int f68, int f72, float f76, int f80, int f84, float f88, float f92, float f96, float f100) : IDummyComponent;
record struct Component104b_3(float f0, int f4, float f8, float f12, float f16, int f20, float f24, float f28, float f32, float f36, float f40, int f44, float f48, int f52, int f56, float f60, int f64, int f68, int f72, int f76, float f80, float f84, float f88, int f92, int f96, int f100) : IDummyComponent;
record struct Component104b_4(int f0, int f4, float f8, int f12, float f16, float f20, float f24, float f28, int f32, float f36, int f40, float f44, float f48, int f52, float f56, float f60, int f64, int f68, int f72, float f76, int f80, int f84, int f88, int f92, float f96, float f100) : IDummyComponent;
record struct Component104b_5(float f0, int f4, int f8, int f12, int f16, int f20, float f24, float f28, int f32, float f36, float f40, int f44, float f48, float f52, float f56, int f60, int f64, float f68, int f72, float f76, float f80, float f84, float f88, float f92, int f96, int f100) : IDummyComponent;
record struct Component104b_6(int f0, float f4, int f8, int f12, int f16, int f20, int f24, float f28, int f32, float f36, float f40, int f44, float f48, int f52, float f56, int f60, int f64, int f68, float f72, int f76, float f80, float f84, int f88, float f92, float f96, int f100) : IDummyComponent;
record struct Component104b_7(float f0, int f4, int f8, int f12, float f16, float f20, float f24, float f28, float f32, int f36, int f40, float f44, int f48, int f52, int f56, float f60, int f64, int f68, int f72, int f76, int f80, int f84, float f88, float f92, float f96, float f100) : IDummyComponent;
record struct Component104b_8(int f0, float f4, float f8, int f12, float f16, float f20, float f24, int f28, int f32, float f36, float f40, int f44, int f48, int f52, int f56, int f60, int f64, float f68, float f72, int f76, float f80, float f84, float f88, int f92, float f96, int f100) : IDummyComponent;
record struct Component104b_9(float f0, int f4, int f8, int f12, int f16, float f20, float f24, int f28, int f32, int f36, float f40, float f44, int f48, float f52, int f56, float f60, float f64, int f68, float f72, float f76, int f80, float f84, int f88, float f92, float f96, float f100) : IDummyComponent;
record struct Component108b_0(int f0, float f4, int f8, int f12, float f16, int f20, float f24, float f28, float f32, float f36, int f40, int f44, float f48, int f52, int f56, float f60, float f64, int f68, float f72, float f76, int f80, int f84, int f88, int f92, float f96, float f100, float f104) : IDummyComponent;
record struct Component108b_1(float f0, float f4, float f8, int f12, int f16, float f20, int f24, int f28, float f32, int f36, int f40, int f44, float f48, float f52, int f56, float f60, float f64, float f68, float f72, float f76, float f80, float f84, float f88, float f92, int f96, int f100, int f104) : IDummyComponent;
record struct Component108b_2(float f0, float f4, float f8, int f12, float f16, float f20, int f24, int f28, int f32, float f36, float f40, float f44, float f48, int f52, float f56, float f60, float f64, float f68, float f72, int f76, float f80, float f84, int f88, int f92, float f96, float f100, int f104) : IDummyComponent;
record struct Component108b_3(float f0, float f4, int f8, int f12, int f16, float f20, float f24, int f28, int f32, int f36, float f40, int f44, float f48, int f52, float f56, float f60, float f64, float f68, float f72, float f76, int f80, int f84, int f88, int f92, float f96, float f100, float f104) : IDummyComponent;
record struct Component108b_4(int f0, float f4, int f8, float f12, float f16, float f20, int f24, int f28, int f32, int f36, float f40, float f44, int f48, int f52, float f56, int f60, float f64, float f68, float f72, float f76, float f80, int f84, int f88, int f92, int f96, float f100, int f104) : IDummyComponent;
record struct Component108b_5(float f0, int f4, float f8, float f12, int f16, int f20, int f24, int f28, float f32, int f36, float f40, float f44, float f48, float f52, float f56, int f60, int f64, int f68, float f72, float f76, int f80, float f84, float f88, int f92, int f96, float f100, float f104) : IDummyComponent;
record struct Component108b_6(int f0, float f4, float f8, int f12, int f16, float f20, int f24, int f28, float f32, float f36, float f40, float f44, int f48, float f52, float f56, float f60, int f64, int f68, int f72, int f76, int f80, float f84, int f88, int f92, int f96, int f100, float f104) : IDummyComponent;
record struct Component108b_7(int f0, int f4, float f8, float f12, float f16, float f20, float f24, float f28, int f32, int f36, float f40, int f44, int f48, int f52, int f56, float f60, float f64, int f68, int f72, int f76, int f80, int f84, int f88, int f92, int f96, float f100, int f104) : IDummyComponent;
record struct Component108b_8(float f0, float f4, int f8, float f12, int f16, float f20, float f24, int f28, float f32, float f36, int f40, float f44, int f48, float f52, float f56, float f60, float f64, float f68, int f72, int f76, int f80, float f84, int f88, int f92, int f96, int f100, float f104) : IDummyComponent;
record struct Component112b_0(int f0, float f4, int f8, float f12, float f16, int f20, int f24, int f28, int f32, float f36, int f40, int f44, int f48, float f52, float f56, float f60, float f64, int f68, float f72, int f76, int f80, float f84, int f88, float f92, float f96, float f100, int f104, int f108) : IDummyComponent;
record struct Component112b_1(float f0, int f4, int f8, int f12, float f16, float f20, float f24, int f28, int f32, int f36, int f40, int f44, int f48, int f52, int f56, int f60, int f64, int f68, int f72, float f76, float f80, int f84, int f88, float f92, float f96, int f100, int f104, int f108) : IDummyComponent;
record struct Component112b_2(int f0, float f4, float f8, int f12, float f16, int f20, float f24, int f28, float f32, int f36, int f40, float f44, float f48, float f52, int f56, int f60, float f64, float f68, float f72, int f76, float f80, float f84, float f88, float f92, float f96, float f100, int f104, int f108) : IDummyComponent;
record struct Component112b_3(float f0, int f4, float f8, int f12, int f16, int f20, float f24, int f28, int f32, float f36, float f40, float f44, int f48, float f52, float f56, float f60, float f64, int f68, float f72, int f76, int f80, int f84, float f88, int f92, float f96, float f100, int f104, int f108) : IDummyComponent;
record struct Component112b_4(float f0, int f4, int f8, int f12, int f16, float f20, int f24, int f28, float f32, int f36, int f40, float f44, float f48, float f52, float f56, float f60, float f64, float f68, int f72, float f76, float f80, int f84, float f88, int f92, float f96, float f100, float f104, int f108) : IDummyComponent;
record struct Component112b_5(int f0, float f4, int f8, float f12, float f16, float f20, float f24, int f28, float f32, float f36, int f40, float f44, int f48, int f52, int f56, float f60, float f64, int f68, float f72, float f76, int f80, int f84, float f88, float f92, int f96, int f100, int f104, int f108) : IDummyComponent;
record struct Component112b_6(int f0, float f4, float f8, int f12, int f16, int f20, int f24, float f28, int f32, int f36, float f40, int f44, float f48, int f52, int f56, int f60, int f64, float f68, float f72, int f76, int f80, float f84, float f88, float f92, int f96, int f100, float f104, float f108) : IDummyComponent;
record struct Component112b_7(float f0, float f4, int f8, int f12, int f16, float f20, int f24, float f28, int f32, float f36, float f40, int f44, float f48, float f52, int f56, int f60, int f64, int f68, float f72, float f76, int f80, float f84, int f88, float f92, float f96, int f100, int f104, int f108) : IDummyComponent;
record struct Component112b_8(float f0, float f4, int f8, float f12, float f16, float f20, int f24, float f28, int f32, float f36, float f40, float f44, float f48, float f52, float f56, int f60, float f64, int f68, int f72, float f76, float f80, float f84, int f88, float f92, float f96, float f100, int f104, int f108) : IDummyComponent;
record struct Component116b_0(float f0, int f4, int f8, int f12, float f16, float f20, int f24, int f28, int f32, float f36, float f40, float f44, float f48, float f52, float f56, int f60, float f64, float f68, int f72, float f76, float f80, int f84, int f88, float f92, float f96, float f100, float f104, float f108, float f112) : IDummyComponent;
record struct Component116b_1(float f0, int f4, int f8, float f12, float f16, float f20, float f24, float f28, float f32, float f36, int f40, int f44, float f48, int f52, float f56, int f60, float f64, float f68, float f72, int f76, int f80, int f84, float f88, float f92, float f96, int f100, float f104, float f108, float f112) : IDummyComponent;
record struct Component116b_2(int f0, int f4, int f8, int f12, float f16, int f20, float f24, int f28, float f32, int f36, int f40, float f44, float f48, int f52, int f56, int f60, int f64, int f68, int f72, int f76, int f80, float f84, int f88, float f92, int f96, int f100, float f104, float f108, float f112) : IDummyComponent;
record struct Component116b_3(float f0, float f4, float f8, float f12, float f16, int f20, float f24, float f28, int f32, int f36, float f40, int f44, int f48, int f52, int f56, float f60, int f64, int f68, int f72, int f76, int f80, float f84, int f88, float f92, int f96, float f100, float f104, float f108, int f112) : IDummyComponent;
record struct Component116b_4(float f0, int f4, float f8, int f12, int f16, float f20, int f24, float f28, int f32, int f36, int f40, float f44, int f48, float f52, int f56, float f60, float f64, int f68, int f72, float f76, int f80, float f84, int f88, int f92, float f96, float f100, int f104, int f108, float f112) : IDummyComponent;
record struct Component116b_5(float f0, float f4, int f8, float f12, float f16, float f20, int f24, int f28, int f32, float f36, float f40, float f44, float f48, int f52, int f56, float f60, float f64, int f68, float f72, float f76, float f80, float f84, int f88, int f92, int f96, float f100, int f104, float f108, float f112) : IDummyComponent;
record struct Component116b_6(float f0, int f4, int f8, int f12, float f16, float f20, float f24, int f28, int f32, float f36, int f40, int f44, int f48, float f52, float f56, float f60, int f64, float f68, int f72, float f76, int f80, int f84, float f88, int f92, float f96, float f100, float f104, float f108, float f112) : IDummyComponent;
record struct Component116b_7(int f0, int f4, float f8, float f12, int f16, int f20, float f24, float f28, int f32, int f36, float f40, float f44, float f48, float f52, float f56, int f60, int f64, int f68, int f72, float f76, float f80, int f84, int f88, int f92, float f96, int f100, float f104, float f108, int f112) : IDummyComponent;
record struct Component116b_8(float f0, int f4, int f8, int f12, int f16, float f20, float f24, float f28, float f32, int f36, float f40, int f44, int f48, int f52, int f56, int f60, float f64, int f68, float f72, float f76, float f80, float f84, float f88, int f92, float f96, int f100, int f104, int f108, int f112) : IDummyComponent;
record struct Component120b_0(int f0, int f4, int f8, int f12, float f16, float f20, float f24, int f28, float f32, float f36, float f40, float f44, int f48, int f52, int f56, int f60, int f64, int f68, float f72, float f76, float f80, float f84, int f88, int f92, int f96, int f100, int f104, int f108, int f112, int f116) : IDummyComponent;
record struct Component120b_1(float f0, float f4, float f8, int f12, int f16, int f20, int f24, int f28, int f32, int f36, float f40, int f44, int f48, int f52, float f56, int f60, float f64, float f68, float f72, int f76, int f80, float f84, float f88, float f92, float f96, int f100, int f104, int f108, int f112, int f116) : IDummyComponent;
record struct Component120b_2(float f0, float f4, float f8, int f12, float f16, int f20, int f24, int f28, int f32, float f36, int f40, float f44, float f48, float f52, int f56, float f60, float f64, float f68, int f72, int f76, int f80, int f84, int f88, int f92, int f96, float f100, float f104, float f108, int f112, float f116) : IDummyComponent;
record struct Component120b_3(float f0, int f4, float f8, int f12, int f16, float f20, float f24, float f28, int f32, float f36, int f40, float f44, float f48, float f52, float f56, int f60, int f64, float f68, float f72, float f76, float f80, float f84, int f88, float f92, float f96, float f100, float f104, float f108, int f112, int f116) : IDummyComponent;
record struct Component120b_4(float f0, float f4, int f8, float f12, float f16, float f20, int f24, float f28, int f32, float f36, float f40, int f44, float f48, float f52, int f56, float f60, float f64, float f68, float f72, float f76, float f80, int f84, int f88, float f92, float f96, float f100, int f104, int f108, int f112, float f116) : IDummyComponent;
record struct Component120b_5(int f0, int f4, float f8, int f12, int f16, int f20, float f24, int f28, int f32, int f36, float f40, int f44, float f48, float f52, float f56, float f60, int f64, float f68, int f72, float f76, int f80, float f84, float f88, int f92, float f96, float f100, int f104, float f108, int f112, float f116) : IDummyComponent;
record struct Component120b_6(float f0, int f4, float f8, int f12, int f16, int f20, int f24, float f28, int f32, int f36, int f40, int f44, int f48, float f52, float f56, float f60, float f64, float f68, int f72, int f76, int f80, float f84, float f88, float f92, int f96, float f100, float f104, float f108, float f112, int f116) : IDummyComponent;
record struct Component120b_7(float f0, float f4, int f8, int f12, int f16, int f20, int f24, int f28, int f32, float f36, int f40, int f44, float f48, int f52, float f56, int f60, int f64, float f68, float f72, int f76, int f80, int f84, float f88, int f92, int f96, int f100, int f104, float f108, float f112, int f116) : IDummyComponent;
record struct Component124b_0(float f0, float f4, float f8, int f12, float f16, int f20, float f24, int f28, int f32, float f36, float f40, float f44, float f48, float f52, int f56, int f60, int f64, int f68, float f72, int f76, int f80, float f84, float f88, int f92, int f96, float f100, int f104, int f108, float f112, float f116, int f120) : IDummyComponent;
record struct Component124b_1(float f0, float f4, float f8, int f12, float f16, float f20, float f24, float f28, int f32, float f36, float f40, int f44, int f48, float f52, float f56, float f60, float f64, float f68, float f72, int f76, float f80, float f84, float f88, float f92, float f96, float f100, float f104, float f108, float f112, int f116, int f120) : IDummyComponent;
record struct Component124b_2(int f0, float f4, int f8, int f12, float f16, int f20, int f24, float f28, int f32, float f36, int f40, float f44, float f48, int f52, int f56, int f60, float f64, float f68, float f72, float f76, float f80, int f84, int f88, float f92, float f96, int f100, int f104, float f108, float f112, float f116, float f120) : IDummyComponent;
record struct Component124b_3(float f0, int f4, float f8, float f12, float f16, int f20, int f24, int f28, int f32, int f36, int f40, float f44, int f48, float f52, int f56, float f60, float f64, int f68, float f72, float f76, int f80, float f84, int f88, int f92, float f96, float f100, int f104, float f108, int f112, float f116, float f120) : IDummyComponent;
record struct Component124b_4(float f0, int f4, int f8, float f12, int f16, int f20, float f24, int f28, int f32, int f36, int f40, int f44, float f48, float f52, float f56, int f60, int f64, int f68, float f72, float f76, int f80, int f84, int f88, float f92, int f96, int f100, float f104, int f108, int f112, int f116, int f120) : IDummyComponent;
record struct Component124b_5(float f0, float f4, float f8, float f12, float f16, int f20, int f24, int f28, float f32, int f36, int f40, float f44, int f48, int f52, int f56, float f60, int f64, float f68, int f72, int f76, int f80, int f84, float f88, float f92, int f96, int f100, float f104, int f108, int f112, int f116, float f120) : IDummyComponent;
record struct Component124b_6(float f0, float f4, float f8, float f12, float f16, float f20, int f24, float f28, int f32, float f36, int f40, int f44, int f48, float f52, float f56, int f60, float f64, float f68, float f72, float f76, float f80, int f84, int f88, int f92, float f96, float f100, int f104, int f108, float f112, float f116, float f120) : IDummyComponent;
record struct Component124b_7(float f0, int f4, float f8, float f12, float f16, float f20, float f24, int f28, int f32, float f36, float f40, float f44, int f48, int f52, float f56, int f60, int f64, int f68, int f72, float f76, int f80, int f84, int f88, int f92, float f96, float f100, int f104, int f108, float f112, int f116, float f120) : IDummyComponent;
record struct Component128b_0(float f0, float f4, float f8, float f12, int f16, int f20, float f24, int f28, float f32, float f36, int f40, float f44, float f48, int f52, int f56, int f60, int f64, int f68, int f72, float f76, float f80, float f84, int f88, float f92, int f96, int f100, int f104, int f108, int f112, float f116, float f120, int f124) : IDummyComponent;
record struct Component128b_1(float f0, int f4, int f8, float f12, int f16, int f20, int f24, int f28, int f32, int f36, float f40, int f44, float f48, float f52, float f56, int f60, int f64, int f68, int f72, float f76, int f80, float f84, float f88, int f92, float f96, float f100, float f104, int f108, float f112, int f116, float f120, float f124) : IDummyComponent;
record struct Component128b_2(int f0, float f4, float f8, int f12, int f16, int f20, int f24, int f28, float f32, float f36, int f40, int f44, int f48, int f52, float f56, float f60, int f64, float f68, float f72, int f76, int f80, float f84, float f88, float f92, float f96, float f100, float f104, int f108, float f112, int f116, float f120, int f124) : IDummyComponent;
record struct Component128b_3(float f0, int f4, float f8, int f12, int f16, int f20, float f24, int f28, float f32, float f36, float f40, float f44, int f48, float f52, int f56, int f60, int f64, float f68, int f72, float f76, float f80, float f84, int f88, float f92, float f96, int f100, float f104, float f108, int f112, int f116, float f120, float f124) : IDummyComponent;
record struct Component128b_4(int f0, int f4, int f8, float f12, float f16, int f20, float f24, float f28, float f32, float f36, float f40, float f44, float f48, float f52, float f56, float f60, float f64, int f68, float f72, float f76, float f80, int f84, int f88, float f92, int f96, int f100, int f104, int f108, float f112, int f116, float f120, float f124) : IDummyComponent;
record struct Component128b_5(int f0, float f4, float f8, float f12, float f16, float f20, float f24, int f28, float f32, float f36, int f40, float f44, int f48, int f52, int f56, float f60, int f64, int f68, int f72, float f76, float f80, float f84, float f88, int f92, float f96, float f100, float f104, float f108, float f112, int f116, int f120, float f124) : IDummyComponent;
record struct Component128b_6(float f0, float f4, float f8, int f12, float f16, float f20, int f24, float f28, float f32, float f36, float f40, int f44, float f48, float f52, int f56, float f60, float f64, int f68, float f72, float f76, int f80, int f84, float f88, int f92, int f96, float f100, float f104, float f108, float f112, float f116, float f120, float f124) : IDummyComponent;
record struct Component128b_7(float f0, int f4, float f8, float f12, float f16, int f20, float f24, int f28, float f32, float f36, float f40, float f44, float f48, float f52, int f56, int f60, int f64, float f68, int f72, int f76, int f80, int f84, float f88, int f92, float f96, float f100, float f104, int f108, int f112, float f116, float f120, int f124) : IDummyComponent;